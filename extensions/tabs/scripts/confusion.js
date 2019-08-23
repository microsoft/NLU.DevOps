// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// d3 and json2html included in HTML script reference
/* global d3, json2html, Matrix, Graph */

// json2html uses curly brace syntax for templating
/* eslint no-template-curly-in-string: "warn" */

// eslint-disable-next-line no-unused-vars
function renderConfusionMatrix (filename, headers) {
  let quadrantCountMax = 0
  let nestData, facetActualIntents, facetExpectedIntents

  d3.json(filename, { headers }).then(function (data) {
    nestData = d3.nest()
      .key(function (d) { return d.resultKind })
      .entries(data)
    facetExpectedIntents = d3.nest()
      .key(function (d) { return d.expectedUtterance.intent })
      .entries(data)
    facetActualIntents = d3.nest()
      .key(function (d) { return d.actualUtterance.intent })
      .entries(data)
    for (let key = 0; key < nestData.length; key++) {
      console.log(nestData[key])
      quadrantCountMax = Math.max(quadrantCountMax, nestData[key].values.length)
    }

    const matrix = new Matrix()
    const graph = new Graph()
    graph.quadrantCountMax = quadrantCountMax

    addGraph(graph)
    addDataPoints(nestData, matrix, graph)
    addFacets('Actual Intents', facetActualIntents)
    addFacets('Expected Intents', facetExpectedIntents)
  })
}

function addFacets (section, data) {
  var slicerDiv = d3.select('#facets').append('div')

  slicerDiv.append('div')
    .attr('class', 'heading')
    .text(section)

  slicerDiv.selectAll('p')
    .data(data)
    .enter()
    .append('div')
    .text(function (d) { return d.key })
}

function addDataPoints (data, matrix, graph) {
  for (let index = 0; index < data.length; index++) {
    var tooltip = d3.select('body').append('div')
      .attr('class', 'tooltip')
      .style('opacity', 0)
    d3.select('svg')
      .selectAll('elements')
      .data(data[index].values)
      .enter()
      .append('circle')
      .attr('r', 5)
      .attr('cx', function (d, i) { return getXConfusionQuadrant(graph, d, i, matrix) })
      .attr('cy', function (d, i) { return getYConfusionQuadrant(graph, d, i) })
      .attr('stroke', function (d, i) { return getDotColor(d, i) })
      .attr('fill', function (d, i) { return getDotColor(d, i) })
      .attr('opacity', 0.5)
      .on('mouseover', function (d, i) {
        d3.selectAll('.active').classed('active', false)
        d3.select('svg')
          .append('circle')
          .attr('cx', getXConfusionQuadrant(graph, d, i, matrix))
          .attr('cy', getYConfusionQuadrant(graph, d, i))
          .attr('stroke', getDotColor(d, i))
          .attr('fill', getDotColor(d, i))
          .attr('opacity', 0.5)
          .classed('active', true)
          .on('click', function () {
            d3.selectAll('.clicked').classed('clicked', false)
            var div = d3.select('#testData')
            div.html(getTestResultHtml(d, matrix))
            d3.selectAll('.active').classed('clicked', true)
          })
          .on('mouseover', function () {
            tooltip.transition()
              .duration(500)
              .style('opacity', 0.9)
            tooltip.html(d.score || 'not scored')
              .style('left', (d3.event.pageX) + 'px')
              .style('top', (d3.event.pageY - 28) + 'px')
          })
          .on('mouseout', function () {
            tooltip.transition()
              .duration(500)
              .style('opacity', 0)
            d3.selectAll('.active').classed('active', false)
          })
      })
  }
}

function addGraph (graph) {
  const yScale = graph.yScale()

  var yAxis = d3.axisLeft().scale(yScale)

  var svg = d3.select('#confusionMatrix').append('svg')
    .attr('viewBox', '0, 0,' + graph.outerWidth + ',' + graph.outerHeight)
    .append('g')
    .attr('transform', 'translate(' + graph.margin.left + ',' + graph.margin.top + ')')

  svg.append('rect')
    .attr('class', 'outer')
    .attr('width', graph.innerWidth)
    .attr('height', graph.innerHeight)

  var g = svg.append('g')
    .attr('transform', 'translate(' + graph.padding.left + ',' + graph.padding.top + ')')

  g.append('g')
    .attr('class', 'y axis')
    .attr('transform', 'translate(0,0)')
    .call(yAxis)

  g.selectAll('line.horizontalGrid').data(yScale.ticks(13))
    .enter()
    .append('line')
    .attr('class', 'horizontalGrid')
    .attr('x1', 0)
    .attr('x2', graph.width)
    .attr('y1', function (d) { return yScale(d) })
    .attr('y2', function (d) { return yScale(d) })
    .attr('fill', 'none')
    .attr('shape-rendering', 'crispEdges')
    .attr('stroke', 'steelblue')
    .attr('stroke-dasharray', '10,10')
    .attr('stroke-width', '1px')

  g.append('text')
    .attr('x', 10)
    .attr('y', 15)
    .text('True Positive')
  g.append('text')
    .attr('x', graph.width - 100)
    .attr('y', 15)
    .text('False Positive')
  g.append('text')
    .attr('x', 10)
    .attr('y', graph.height - 5)
    .text('False Negative')
  g.append('text')
    .attr('x', graph.width - 100)
    .attr('y', graph.height - 5)
    .text('True Negative')
  g.append('line')
    .attr('x1', 0)
    .attr('y1', graph.height / 2)
    .attr('x2', graph.width)
    .attr('y2', graph.height / 2)
    .attr('stroke', 'steelblue')
  g.append('line')
    .attr('x1', graph.width / 2)
    .attr('y1', 0)
    .attr('x2', graph.width / 2)
    .attr('y2', graph.height)
    .attr('stroke', 'steelblue')
}

function getTestResultHtml (test, matrix) {
  console.log('getTestResults')
  var t = {
    test: {
      '<>': 'div',
      class: 'col-8',
      html: [
        { '<>': 'div', class: 'heading', html: 'Model Statistics' },
        {
          '<>': 'div',
          class: 'row',
          html: [
            { '<>': 'div', class: 'col-3', html: 'Precision: ' + matrix.getPrecision() },
            { '<>': 'div', class: 'col-3', html: 'Recall: ' + matrix.getRecall() },
            { '<>': 'div', class: 'col-3', html: 'F1: ' + matrix.getF1() },
            { '<>': 'div', class: 'col-3', html: 'Accuracy: ' + matrix.getAccuracy() }
          ]
        },
        { '<>': 'br' },
        { '<>': 'div', class: 'heading', html: '${testName}' },
        { '<>': 'p', html: 'Target: ${targetKind}' },
        { '<>': 'p', html: '${resultKind}: ${because}' },
        {
          '<>': 'div',
          class: 'row',
          html: [
            {
              '<>': 'table',
              class: 'table',
              html: [
                {
                  '<>': 'thead',
                  html: [
                    {
                      '<>': 'tr',
                      html: [
                        { '<>': 'th', scope: 'col', html: 'Expected Utterance' },
                        { '<>': 'th', scope: 'col', html: 'Actual Utterance' }
                      ]
                    }
                  ]
                },
                {
                  '<>': 'tbody',
                  html: [
                    {
                      '<>': 'tr',
                      html: [
                        {
                          '<>': 'td',
                          html: function () {
                            var innerHtml = json2html.transform(this.expectedUtterance, t.utterance)
                            var actual = false
                            return styleEntities(innerHtml, this.expectedUtterance, this.resultKind, actual)
                          }
                        },
                        {
                          '<>': 'td',
                          html: function () {
                            var innerHtml = json2html.transform(this.actualUtterance, t.utterance)
                            var actual = true
                            return styleEntities(innerHtml, this.actualUtterance, this.resultKind, actual)
                          }
                        }
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    },
    utterance: {
      '<>': 'p',
      html: [
        { '<>': 'p', html: 'Text: ${text}' },
        { '<>': 'p', html: 'Intent: ${intent}' }
      ]
    }
  }
  return json2html.transform(test, t.test)
}

function styleEntities (html, utterance, resultKind, actualUtterance) {
  var styleColor = 'rgba(0,255,0,.1)' // 50% transparent green
  if (
    (actualUtterance && (resultKind === 'FalsePositive')) || (!actualUtterance && (resultKind === 'FalseNegative'))
  ) { styleColor = 'rgba(255,0,0,.1)' } // 50% transparent red
  if (utterance.entities !== undefined) {
    if (utterance.entities.length > 0) {
      var searchText = utterance.entities[0].matchText
      html = html.replace(searchText, '<span style="background-color: ' + styleColor + '; inline">' + searchText + '</span>')
    }
  }
  return html
}

function getDotColor (d, i) {
  if ((d.resultKind === 'TruePositive') || (d.resultKind === 'TrueNegative')) {
    return 'green'
  }
  return 'red'
}

function getXConfusionQuadrant (graph, d, i, matrix) {
  const xScale = graph.xScale()
  switch (d.resultKind) {
    case 'TruePositive': matrix.tp += 1
      break
    case 'FalsePositive': matrix.fp += 1
      break
    case 'TrueNegative': matrix.tn += 1
      break
    case 'FalseNegative': matrix.fn += 1
      break
  }
  var x = 0
  if ((d.resultKind === 'TruePositive') || (d.resultKind === 'FalseNegative')) {
    x = xScale(i) + (graph.margin.left + graph.padding.left + 5)
  } else {
    x = xScale(i + graph.quadrantCountMax) + (graph.margin.left + graph.padding.left + 10)
  }
  return x
}

function getYConfusionQuadrant (graph, d, i) {
  const yScale = graph.yScale()
  var value = d.score || 1
  var retVal = 0
  if ((d.resultKind === 'TrueNegative') || (d.resultKind === 'FalseNegative')) {
    var negValue = 0 - value
    retVal = yScale(negValue)
  } else {
    retVal = yScale(value)
  }
  return retVal + (graph.margin.top + graph.padding.top)
}
