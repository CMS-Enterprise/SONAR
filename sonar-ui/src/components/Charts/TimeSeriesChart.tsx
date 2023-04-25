import React from 'react';
import Chart from 'react-apexcharts';
import { ApexOptions } from "apexcharts";

const TimeSeriesChart: React.FC<{
  healthCheckName:string,
  timeSeriesData:number[][]
}> = ({ healthCheckName, timeSeriesData }) => {

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
        autoSelected: 'zoom'
      }
    },
    dataLabels: {
      enabled: false
    },
    xaxis: {
      type: 'datetime',
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
          '<div ><b>'+ healthCheckName + '</b></div>' +
          '<div><b>HealthStatus</b>: ' + series[seriesIndex][dataPointIndex] + '</div>' +
        '</div>';
      }
    }
  }

  const updateData = (timeline:string) => {
    const SEC = 1000;
    const MIN = 60 * SEC;
    const currentTime = new Date().getTime()

    switch (timeline) {
      case '30s':
        ApexCharts.exec(
          healthCheckName,
          'zoomX',
          currentTime,
          currentTime + (30 * SEC)
        )
        break
      case '1m':
        ApexCharts.exec(
          healthCheckName,
          'zoomX',
          currentTime,
          currentTime + (1 * MIN)
        )
        break
      case '5m':
        ApexCharts.exec(
          healthCheckName,
          'zoomX',
          currentTime,
          currentTime + (5 * MIN)
        )
        break
      case 'all':
        // Maximum time of 10 minutes
        ApexCharts.exec(
          healthCheckName,
          'zoomX',
          currentTime,
          currentTime + (10 * MIN)
        )
        break
      default:
    }
  }

  return (
    <div>
      <div className="toolbar">
        <button id="30s" onClick={() => updateData('30s')}>30s</button>
        &nbsp;
        <button id="1m" onClick={() => updateData('1m')}>1m</button>
        &nbsp;
        <button id="5m" onClick={() => updateData('5m')}>5m</button>
        &nbsp;
        <button id="all" onClick={() => updateData('all')}>All</button>
      </div>

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
export default TimeSeriesChart;