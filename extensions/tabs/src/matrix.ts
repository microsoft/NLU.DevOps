// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// eslint-disable-next-line no-unused-vars
class Matrix {
  constructor() {
    this.p  = 0;
    this.n  = 0;
    this.tp = 0;
    this.tn = 0;
    this.fp = 0;
    this.fn = 0;
  }

  public getAccuracy() {
    const accuracy = (this.tp + this.tn) / ((this.tp + this.fn) + (this.fp + this.tn))
    return Math.round(accuracy * 100) / 100
  }

  public getF1() {
    const f1 = 2 * this.tp / (2 * this.tp + this.fp + this.fn)
    return Math.round(f1 * 100) / 100
  }

  public getPrecision() {
    const precision = this.tp / (this.tp + this.fp)
    return Math.round(precision * 100) / 100
  }

  public getRecall() {
    const recall = this.tp / (this.tp + this.fn)
    return Math.round(recall * 100) / 100
  }
}
