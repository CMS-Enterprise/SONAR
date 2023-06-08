import React, { useState } from 'react';
import Chart from 'react-apexcharts';
import { ApexOptions } from "apexcharts";
import { IHealthCheckCondition, IHealthCheckDefinition } from 'types';

const HealthStatusDataTimeSeriesChart: React.FC<{
  svcDefinitions: IHealthCheckDefinition | null,
  healthCheckName: string,
  timeSeriesData: number[][]
}> = ({ svcDefinitions, healthCheckName, timeSeriesData }) => {
  const [displayAnnotation, setDisplayAnnotation] = useState(false);

  const chartSeries = {
    series: [{
      data: [...timeSeriesData]
    }]
  };

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
        return '<div>' +
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
    svcDefinitions?.conditions.forEach((c: IHealthCheckCondition) => (
      ApexCharts.exec(healthCheckName, 'addYaxisAnnotation', {
        y: c.threshold,
        borderColor: 'grey',
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
      }, true)
    ))
  };

  const clearAnnotations = () => {
    setDisplayAnnotation(false);

    //TODO BATAPI-241
    ApexCharts.exec(healthCheckName, 'clearAnnotations');
    ApexCharts.exec(healthCheckName, 'clearAnnotations');
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
      { displayAnnotation?
        <button id="hideAnnotation" onClick={() => clearAnnotations()}>Hide Annotations</button>:
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
export default HealthStatusDataTimeSeriesChart;
