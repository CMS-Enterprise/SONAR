import { useTheme } from '@emotion/react';
import React, { useState } from 'react';
import Chart from 'react-apexcharts';
import { ApexOptions } from "apexcharts";
import { IHealthCheckCondition, IHealthCheckDefinition, IHealthCheckHttpCondition } from 'types';
import { HealthStatus } from '../../../../../api/data-contracts';
import { getStatusColors } from '../../../../../helpers/StyleHelper';
import { v4 as uuidv4 } from 'uuid';

function hmsToSecondsOnly(str: string | undefined) {
  if (!str) {
    return 0;
  }

  const p = str.split(':');
  let s = 0, m = 1;

  while (p.length > 0) {
    s += m * parseInt(p.pop() as string, 10);
    m *= 60;
  }

  return s;
}

const MetricHealthCheckDataTimeSeriesChart: React.FC<{
  svcDefinitions: IHealthCheckDefinition | null,
  healthCheckName: string,
  timeSeriesData: number[][],
  responseTimeData: IHealthCheckHttpCondition | undefined
}> = ({ svcDefinitions, healthCheckName, timeSeriesData, responseTimeData }) => {

  const theme = useTheme();
  const [displayAnnotation, setDisplayAnnotation] = useState(false);
  const chartSeries = {
    series: [{
      data: [...timeSeriesData]
    }]
  };
  const [renderedAnnotations, setRenderedAnnotations] = useState<string[]>([]);

  // function that creates annotation object based on type of health check (response time or metric)
  const renderAnnotations = () => {
    const annotations = responseTimeData ? [
      {
        id: responseTimeData.status + '-' + uuidv4(),
        y: hmsToSecondsOnly(responseTimeData.responseTime),
        borderColor: getStatusColors(theme, HealthStatus[responseTimeData.status as keyof typeof HealthStatus]),
        borderWidth: 3,
        strokeDashArray: 4,
        label: {
          text: `Response Time Threshold`,
          position: 'left',
          textAnchor: 'start',
          offsetX: 10,
          borderColor: 'grey',
          style: {
            color: 'white',
            background: 'grey',
          },
        }
      }
    ] : svcDefinitions?.conditions.map((c: IHealthCheckCondition) => (
      {
        id: c.status + '-' + uuidv4(),
        y: c.threshold,
        borderColor: getStatusColors(theme, HealthStatus[c.status as keyof typeof HealthStatus]),
        borderWidth: 3,
        strokeDashArray: 4,
        label: {
          text: `${c.status}`,
          position: 'left',
          textAnchor: 'start',
          offsetX: 10,
          borderColor: 'grey',
          style: {
            color: 'white',
            background: 'grey',
          },
        }
      }
    )) ?? [];
    annotations.forEach((annotation: YAxisAnnotations) => {
      ApexCharts.exec(healthCheckName, 'addYaxisAnnotation', annotation, true)
    })
    setRenderedAnnotations(annotations.map(e => e.id));
  }

  const chartOptions:ApexOptions = {
    chart: {
      id: healthCheckName,
      type: 'area',
      stacked: false,
      height: 350,
      zoom: {
        type: 'x',
        enabled: true,
        autoScaleYaxis: true
      },
      toolbar: {
        tools: {
          reset: false
        }
      }
    },
    dataLabels: {
      enabled: false
    },
    xaxis: {
      type: 'datetime',
      labels: {
        datetimeUTC: true
      }
    },
    yaxis: {
      min: 0,
      tickAmount: 5
    },
    tooltip: {
      x: {
        format: 'yyyy-MM-dd hh:mm:ss'
      },
      custom: function({series, seriesIndex, dataPointIndex, w}) {
        return responseTimeData ?
          '<div>' +
          '<div><b>Response Time</b>: ' + series[seriesIndex][dataPointIndex] + '</div>' +
          '</div>' :
          '<div>' +
          '<div><b>'+ healthCheckName + '</b></div>' +
          '<div><b>HealthStatus</b>: ' + series[seriesIndex][dataPointIndex] + '</div>' +
          '</div>';
      }
    }
  }

  const updateData = (timeline:string) => {
    const SEC = 1000;
    const MIN = 60 * SEC;
    const latestTimestamp = new Date(timeSeriesData[0][0]).getTime();

    switch (timeline) {
      case '30s':
        ApexCharts.exec(
          healthCheckName,
          'zoomX',
          latestTimestamp - (30 * SEC),
          latestTimestamp
        )
        break
      case '1m':
        ApexCharts.exec(
          healthCheckName,
          'zoomX',
          latestTimestamp - (1 * MIN),
          latestTimestamp
        )
        break
      case '5m':
        ApexCharts.exec(
          healthCheckName,
          'zoomX',
          latestTimestamp - (5 * MIN),
          latestTimestamp
        )
        break
      case 'all':
        ApexCharts.exec(
          healthCheckName,
          'resetSeries'
        )
        break
      default:
    }
  }

  const displayAnnotations = () => {
    setDisplayAnnotation(true);
    renderAnnotations();
  };

  const clearAnnotations = () => {
    renderedAnnotations.forEach(id =>
      ApexCharts.exec(healthCheckName, 'removeAnnotation', id)
    );
    setRenderedAnnotations([]);
    setDisplayAnnotation(false);
  }

  return (
    <div>
      <button id="30s" onClick={() => updateData('30s')}>30s</button>
      &nbsp;
      <button id="1m" onClick={() => updateData('1m')}>1m</button>
      &nbsp;
      <button id="5m" onClick={() => updateData('5m')}>5m</button>
      &nbsp;
      <button id="all" onClick={() => updateData('all')}>All</button>
      &nbsp;
      { displayAnnotation ?
        <button id="hideAnnotation" onClick={() => clearAnnotations()}>Hide Annotations</button> :
        <button id="displayAnnotation" onClick={() => displayAnnotations()}>Display Annotations</button>
      }

      <Chart
        options={chartOptions}
        series={chartSeries.series}
        type='area'
        width='100%'
        height='400'
      />
    </div>
  )
}
export default MetricHealthCheckDataTimeSeriesChart;
