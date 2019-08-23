// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/* global d3 */

// eslint-disable-next-line no-unused-vars
class Graph {
  constructor () {
    this.margin = { top: 20, right: 20, bottom: 20, left: 20 }
    this.padding = { top: 60, right: 60, bottom: 60, left: 60 }
    this.outerWidth = 960
    this.outerHeight = 500
    this.innerWidth = this.outerWidth - this.margin.left - this.margin.right
    this.innerHeight = this.outerHeight - this.margin.top - this.margin.bottom
    this.width = this.innerWidth - this.padding.left - this.padding.right
    this.height = this.innerHeight - this.padding.top - this.padding.bottom
    this.quadrantCountMax = 0
  }

  yScale () {
    return d3.scaleLinear()
      .domain([1.2, -1.2])
      .range([0, this.height])
  }

  xScale () {
    return d3.scaleLinear()
      .domain([0, this.quadrantCountMax * 2])
      .range([0, this.width - 10])
  }
}
