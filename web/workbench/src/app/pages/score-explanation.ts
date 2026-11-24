import { Component, input } from '@angular/core';

@Component({
  selector: 'mf-score-explanation',
  template: `
    <div class="reason-codes">
      @for (code of reasonCodes(); track code) {
        <span class="chip">{{ code }}</span>
      }
    </div>
  `
})
export class ScoreExplanation {
  readonly reasonCodes = input.required<string[]>();
}
