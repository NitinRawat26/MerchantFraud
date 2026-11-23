using System.Globalization;
using System.Text.Json;
using MerchantFraud.Core.Domain;
using MerchantFraud.Core.Scoring;
using Microsoft.Data.Sqlite;

namespace MerchantFraud.Persistence;

public sealed class FraudStore : IDisposable
{
    private readonly SqliteConnection _connection;

    public FraudStore(string connectionString)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        Initialize();
    }

    public void Dispose() => _connection.Dispose();

    public void UpsertMerchant(Merchant merchant)
    {
        ExecuteNonQuery(
            """
            INSERT INTO merchants (id, legal_name, trading_name, country, website_url, lifecycle)
            VALUES (@id, @legal_name, @trading_name, @country, @website_url, @lifecycle)
            ON CONFLICT(id) DO UPDATE SET
                legal_name = excluded.legal_name,
                trading_name = excluded.trading_name,
                country = excluded.country,
                website_url = excluded.website_url,
                lifecycle = excluded.lifecycle;
            """,
            P("@id", merchant.Id),
            P("@legal_name", merchant.LegalName),
            P("@trading_name", merchant.TradingName),
            P("@country", merchant.Country),
            P("@website_url", merchant.WebsiteUrl),
            P("@lifecycle", merchant.Lifecycle.ToString()));
    }

    public void UpsertTransaction(FraudTransaction transaction)
    {
        ExecuteNonQuery(
            """
            INSERT INTO transactions (
                id, merchant_id, occurred_at, amount, currency, channel, status,
                card_brand, card_bin, card_last4, customer_id, device_id, ip_address, external_ref
            )
            VALUES (
                @id, @merchant_id, @occurred_at, @amount, @currency, @channel, @status,
                @card_brand, @card_bin, @card_last4, @customer_id, @device_id, @ip_address, @external_ref
            )
            ON CONFLICT(id) DO UPDATE SET
                merchant_id = excluded.merchant_id,
                occurred_at = excluded.occurred_at,
                amount = excluded.amount,
                currency = excluded.currency,
                channel = excluded.channel,
                status = excluded.status,
                card_brand = excluded.card_brand,
                card_bin = excluded.card_bin,
                card_last4 = excluded.card_last4,
                customer_id = excluded.customer_id,
                device_id = excluded.device_id,
                ip_address = excluded.ip_address,
                external_ref = excluded.external_ref;
            """,
            P("@id", transaction.Id),
            P("@merchant_id", transaction.MerchantId),
            P("@occurred_at", FormatDateTimeOffset(transaction.OccurredAt)),
            P("@amount", FormatDecimal(transaction.Amount.Amount)),
            P("@currency", transaction.Amount.Currency),
            P("@channel", transaction.Channel.ToString()),
            P("@status", transaction.Status.ToString()),
            P("@card_brand", transaction.CardBrand),
            P("@card_bin", transaction.CardBin),
            P("@card_last4", transaction.CardLast4),
            P("@customer_id", transaction.CustomerId),
            P("@device_id", transaction.DeviceId),
            P("@ip_address", transaction.IpAddress),
            P("@external_ref", transaction.ExternalRef));
    }

    public void UpsertDispute(FraudDispute dispute)
    {
        ExecuteNonQuery(
            """
            INSERT INTO disputes (
                id, transaction_id, merchant_id, opened_at, amount, currency, reason, status, closed_at, external_ref
            )
            VALUES (
                @id, @transaction_id, @merchant_id, @opened_at, @amount, @currency, @reason, @status, @closed_at, @external_ref
            )
            ON CONFLICT(id) DO UPDATE SET
                transaction_id = excluded.transaction_id,
                merchant_id = excluded.merchant_id,
                opened_at = excluded.opened_at,
                amount = excluded.amount,
                currency = excluded.currency,
                reason = excluded.reason,
                status = excluded.status,
                closed_at = excluded.closed_at,
                external_ref = excluded.external_ref;
            """,
            P("@id", dispute.Id),
            P("@transaction_id", dispute.TransactionId),
            P("@merchant_id", dispute.MerchantId),
            P("@opened_at", FormatDateTimeOffset(dispute.OpenedAt)),
            P("@amount", FormatDecimal(dispute.Amount.Amount)),
            P("@currency", dispute.Amount.Currency),
            P("@reason", dispute.Reason),
            P("@status", dispute.Status.ToString()),
            P("@closed_at", dispute.ClosedAt.HasValue ? FormatDateTimeOffset(dispute.ClosedAt.Value) : null),
            P("@external_ref", dispute.ExternalRef));
    }

    public void SaveScoreResult(FraudScoreResult scoreResult)
    {
        ExecuteNonQuery(
            """
            INSERT OR REPLACE INTO score_results (
                id, subject_type, subject_id, score_value, band, reason_codes, scored_at, model_version
            )
            VALUES (
                @id, @subject_type, @subject_id, @score_value, @band, @reason_codes, @scored_at, @model_version
            );
            """,
            P("@id", scoreResult.Id),
            P("@subject_type", scoreResult.SubjectType.ToString()),
            P("@subject_id", scoreResult.SubjectId),
            P("@score_value", scoreResult.Score.Value),
            P("@band", scoreResult.Score.Band.ToString()),
            P("@reason_codes", JsonSerializer.Serialize(scoreResult.Score.ReasonCodes)),
            P("@scored_at", FormatDateTimeOffset(scoreResult.ScoredAt)),
            P("@model_version", scoreResult.ModelVersion));
    }

    public void UpsertCase(FraudCase fraudCase)
    {
        ExecuteNonQuery(
            """
            INSERT INTO cases (
                id, merchant_id, title, priority, status, assigned_to, opened_at, closed_at, external_ref
            )
            VALUES (
                @id, @merchant_id, @title, @priority, @status, @assigned_to, @opened_at, @closed_at, @external_ref
            )
            ON CONFLICT(id) DO UPDATE SET
                merchant_id = excluded.merchant_id,
                title = excluded.title,
                priority = excluded.priority,
                status = excluded.status,
                assigned_to = excluded.assigned_to,
                opened_at = excluded.opened_at,
                closed_at = excluded.closed_at,
                external_ref = excluded.external_ref;
            """,
            P("@id", fraudCase.Id),
            P("@merchant_id", fraudCase.MerchantId),
            P("@title", fraudCase.Title),
            P("@priority", fraudCase.Priority.ToString()),
            P("@status", fraudCase.Status.ToString()),
            P("@assigned_to", fraudCase.AssignedTo),
            P("@opened_at", fraudCase.OpenedAt.HasValue ? FormatDateTimeOffset(fraudCase.OpenedAt.Value) : null),
            P("@closed_at", fraudCase.ClosedAt.HasValue ? FormatDateTimeOffset(fraudCase.ClosedAt.Value) : null),
            P("@external_ref", fraudCase.ExternalRef));
    }

    public void AddAuditEvent(
        string actor,
        string action,
        string entityType,
        string entityId,
        string? details = null,
        DateTimeOffset? occurredAt = null,
        string? id = null)
    {
        ExecuteNonQuery(
            """
            INSERT INTO audit_events (id, occurred_at, actor, action, entity_type, entity_id, details)
            VALUES (@id, @occurred_at, @actor, @action, @entity_type, @entity_id, @details);
            """,
            P("@id", string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id.Trim()),
            P("@occurred_at", FormatDateTimeOffset(occurredAt ?? DateTimeOffset.UtcNow)),
            P("@actor", Required(actor, nameof(actor))),
            P("@action", Required(action, nameof(action))),
            P("@entity_type", Required(entityType, nameof(entityType))),
            P("@entity_id", Required(entityId, nameof(entityId))),
            P("@details", details));
    }

    public IReadOnlyList<Merchant> GetMerchants()
    {
        return Query(
            "SELECT id, legal_name, trading_name, country, website_url, lifecycle FROM merchants ORDER BY id;",
            reader => new Merchant(
                GetString(reader, "id"),
                GetString(reader, "legal_name"),
                GetNullableString(reader, "trading_name"),
                GetNullableString(reader, "country"),
                GetNullableString(reader, "website_url"),
                Enum.Parse<MerchantLifecycle>(GetString(reader, "lifecycle"), ignoreCase: true)));
    }

    public IReadOnlyList<FraudTransaction> GetTransactions(string merchantId)
    {
        return Query(
            "SELECT id, merchant_id, occurred_at, amount, currency, channel, status, card_brand, card_bin, card_last4, customer_id, device_id, ip_address, external_ref FROM transactions WHERE merchant_id = @merchant_id ORDER BY occurred_at, id;",
            MapTransaction,
            P("@merchant_id", merchantId));
    }

    public IReadOnlyList<FraudDispute> GetDisputes(string merchantId)
    {
        return Query(
            "SELECT id, transaction_id, merchant_id, opened_at, amount, currency, reason, status, closed_at, external_ref FROM disputes WHERE merchant_id = @merchant_id ORDER BY opened_at, id;",
            reader => new FraudDispute(
                GetString(reader, "id"),
                GetString(reader, "transaction_id"),
                GetString(reader, "merchant_id"),
                GetDateTimeOffset(reader, "opened_at"),
                new Money(GetDecimal(reader, "amount"), GetString(reader, "currency")),
                GetString(reader, "reason"),
                Enum.Parse<DisputeStatus>(GetString(reader, "status"), ignoreCase: true),
                GetNullableDateTimeOffset(reader, "closed_at"),
                GetNullableString(reader, "external_ref")),
            P("@merchant_id", merchantId));
    }

    public IReadOnlyList<FraudScoreResult> GetScoreResults(string subjectId)
    {
        return Query(
            "SELECT id, subject_type, subject_id, score_value, reason_codes, scored_at, model_version FROM score_results WHERE subject_id = @subject_id ORDER BY scored_at, id;",
            reader =>
            {
                var reasonCodes = JsonSerializer.Deserialize<string[]>(GetString(reader, "reason_codes")) ?? Array.Empty<string>();
                var score = new FraudRiskScore(GetInt32(reader, "score_value"), reasonCodes);
                return new FraudScoreResult(
                    GetString(reader, "id"),
                    Enum.Parse<FraudScoreSubjectType>(GetString(reader, "subject_type"), ignoreCase: true),
                    GetString(reader, "subject_id"),
                    score,
                    GetDateTimeOffset(reader, "scored_at"),
                    GetNullableString(reader, "model_version"));
            },
            P("@subject_id", subjectId));
    }

    public IReadOnlyList<FraudCase> GetCases(string merchantId)
    {
        return Query(
            "SELECT id, merchant_id, title, priority, status, assigned_to, opened_at, closed_at, external_ref FROM cases WHERE merchant_id = @merchant_id ORDER BY opened_at, id;",
            reader => new FraudCase(
                GetString(reader, "id"),
                GetString(reader, "merchant_id"),
                GetString(reader, "title"),
                Enum.Parse<CasePriority>(GetString(reader, "priority"), ignoreCase: true),
                Enum.Parse<CaseStatus>(GetString(reader, "status"), ignoreCase: true),
                GetNullableString(reader, "assigned_to"),
                GetNullableDateTimeOffset(reader, "opened_at"),
                GetNullableDateTimeOffset(reader, "closed_at"),
                GetNullableString(reader, "external_ref")),
            P("@merchant_id", merchantId));
    }

    public IReadOnlyList<FraudAuditEvent> GetAuditEvents(string? entityId = null)
    {
        var sql = "SELECT id, occurred_at, actor, action, entity_type, entity_id, details FROM audit_events";
        var parameters = Array.Empty<SqliteParameter>();
        if (!string.IsNullOrWhiteSpace(entityId))
        {
            sql += " WHERE entity_id = @entity_id";
            parameters = new[] { P("@entity_id", entityId) };
        }

        sql += " ORDER BY occurred_at, id;";
        return Query(
            sql,
            reader => new FraudAuditEvent(
                GetString(reader, "id"),
                GetDateTimeOffset(reader, "occurred_at"),
                GetString(reader, "actor"),
                GetString(reader, "action"),
                GetString(reader, "entity_type"),
                GetString(reader, "entity_id"),
                GetNullableString(reader, "details")),
            parameters);
    }

    private FraudTransaction MapTransaction(SqliteDataReader reader) =>
        new(
            GetString(reader, "id"),
            GetString(reader, "merchant_id"),
            GetDateTimeOffset(reader, "occurred_at"),
            new Money(GetDecimal(reader, "amount"), GetString(reader, "currency")),
            Enum.Parse<TransactionChannel>(GetString(reader, "channel"), ignoreCase: true),
            Enum.Parse<TransactionStatus>(GetString(reader, "status"), ignoreCase: true),
            GetNullableString(reader, "card_brand"),
            GetNullableString(reader, "card_bin"),
            GetNullableString(reader, "card_last4"),
            GetNullableString(reader, "customer_id"),
            GetNullableString(reader, "device_id"),
            GetNullableString(reader, "ip_address"),
            GetNullableString(reader, "external_ref"));

    private void Initialize()
    {
        Execute("CREATE TABLE IF NOT EXISTS merchants (id TEXT PRIMARY KEY, legal_name TEXT NOT NULL, trading_name TEXT NULL, country TEXT NULL, website_url TEXT NULL, lifecycle TEXT NOT NULL);");
        Execute("CREATE TABLE IF NOT EXISTS transactions (id TEXT PRIMARY KEY, merchant_id TEXT NOT NULL, occurred_at TEXT NOT NULL, amount TEXT NOT NULL, currency TEXT NOT NULL, channel TEXT NOT NULL, status TEXT NOT NULL, card_brand TEXT NULL, card_bin TEXT NULL, card_last4 TEXT NULL, customer_id TEXT NULL, device_id TEXT NULL, ip_address TEXT NULL, external_ref TEXT NULL);");
        Execute("CREATE INDEX IF NOT EXISTS ix_transactions_merchant ON transactions(merchant_id);");
        Execute("CREATE TABLE IF NOT EXISTS disputes (id TEXT PRIMARY KEY, transaction_id TEXT NOT NULL, merchant_id TEXT NOT NULL, opened_at TEXT NOT NULL, amount TEXT NOT NULL, currency TEXT NOT NULL, reason TEXT NOT NULL, status TEXT NOT NULL, closed_at TEXT NULL, external_ref TEXT NULL);");
        Execute("CREATE INDEX IF NOT EXISTS ix_disputes_merchant ON disputes(merchant_id);");
        Execute("CREATE TABLE IF NOT EXISTS score_results (id TEXT PRIMARY KEY, subject_type TEXT NOT NULL, subject_id TEXT NOT NULL, score_value INTEGER NOT NULL, band TEXT NOT NULL, reason_codes TEXT NOT NULL, scored_at TEXT NOT NULL, model_version TEXT NULL);");
        Execute("CREATE INDEX IF NOT EXISTS ix_score_results_subject ON score_results(subject_id);");
        Execute("CREATE TABLE IF NOT EXISTS cases (id TEXT PRIMARY KEY, merchant_id TEXT NOT NULL, title TEXT NOT NULL, priority TEXT NOT NULL, status TEXT NOT NULL, assigned_to TEXT NULL, opened_at TEXT NULL, closed_at TEXT NULL, external_ref TEXT NULL);");
        Execute("CREATE INDEX IF NOT EXISTS ix_cases_merchant ON cases(merchant_id);");
        Execute("CREATE TABLE IF NOT EXISTS audit_events (id TEXT PRIMARY KEY, occurred_at TEXT NOT NULL, actor TEXT NOT NULL, action TEXT NOT NULL, entity_type TEXT NOT NULL, entity_id TEXT NOT NULL, details TEXT NULL);");
        Execute("CREATE INDEX IF NOT EXISTS ix_audit_events_entity ON audit_events(entity_type, entity_id);");
    }

    private void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private void ExecuteNonQuery(string sql, params SqliteParameter[] parameters)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        command.ExecuteNonQuery();
    }

    private List<T> Query<T>(string sql, Func<SqliteDataReader, T> map, params SqliteParameter[] parameters)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        using var reader = command.ExecuteReader();
        var results = new List<T>();
        while (reader.Read())
        {
            results.Add(map(reader));
        }

        return results;
    }

    private static SqliteParameter P(string name, object? value) => new() { ParameterName = name, Value = value ?? DBNull.Value };

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string FormatDateTimeOffset(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    private static string FormatDecimal(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string GetString(SqliteDataReader reader, string name) => reader.GetString(reader.GetOrdinal(name));

    private static string? GetNullableString(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static decimal GetDecimal(SqliteDataReader reader, string name) => decimal.Parse(GetString(reader, name), CultureInfo.InvariantCulture);

    private static int GetInt32(SqliteDataReader reader, string name) => reader.GetInt32(reader.GetOrdinal(name));

    private static DateTimeOffset GetDateTimeOffset(SqliteDataReader reader, string name) =>
        DateTimeOffset.Parse(GetString(reader, name), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static DateTimeOffset? GetNullableDateTimeOffset(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : DateTimeOffset.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
