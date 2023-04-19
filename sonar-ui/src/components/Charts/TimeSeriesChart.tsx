import React, { useEffect } from 'react';
import Chart from 'react-apexcharts';
import { ApexOptions } from "apexcharts";
import { Table, TableBody, TableCaption, TableCell, TableHead, TableRow } from '@cmsgov/design-system';

const TimeSeriesChart: React.FC<{
  timeSeriesData:number[][]
}> = ({ timeSeriesData }) => {

  const chartSeries = {
    series: [{
      data: [...timeSeriesData]
    }]
  };

  //Code
  const chartOptions = {
    options:{
      chart: {
        stacked: false,
        height: 350,

      },
      xaxis: {
        type: 'datetime',
        labels: {
          format: 'yyyy-MM-ddThh:mm:ss.fffZ',
        }
      }
    },
    selection: '1m'
  };

  const testOptions:ApexOptions = {
    chart: {
      id: 'area-datetime',
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
  }

  const updateData = (timeline:string) => {
    const MS = 1000;
    const MIN = 60 * MS;
    const HR = 60 * MIN;
    const currentTime = new Date().getTime()
    chartOptions.selection = timeline;

    switch (timeline) {
      case '1m':
        ApexCharts.exec(
          'area-datetime',
          'zoomX',
          currentTime,
          currentTime + (1 * MIN)
        )
        break
      case '5m':
        ApexCharts.exec(
          'area-datetime',
          'zoomX',
          currentTime,
          currentTime + (5 * MIN)
        )
        break
      case '30m':
        ApexCharts.exec(
          'area-datetime',
          'zoomX',
          currentTime,
          currentTime + (30 * MIN)
        )
        break
      case '1h':
        ApexCharts.exec(
          'area-datetime',
          'zoomX',
          currentTime,
          currentTime + (1 * HR)
        )
        break
      case '24h':
        ApexCharts.exec(
          'area-datetime',
          'zoomX',
          currentTime,
          currentTime + (24 * HR)
        )
        break
      default:
    }
  }

  return (
    <div>
      <div className="toolbar">
        <button id="1m" onClick={() => updateData('1m')} className={(chartOptions.selection === '1m' ? 'active' : '')}>
          1m
        </button>
        &nbsp;
        <button id="5m" onClick={() => updateData('5m')} className={(chartOptions.selection === '5m' ? 'active' : '')}>
          5m
        </button>
        &nbsp;
        <button id="30m" onClick={() => updateData('30m')} className={(chartOptions.selection === '30m' ? 'active' : '')}>
          30m
        </button>
        &nbsp;
        <button id="1h" onClick={() => updateData('1h')} className={(chartOptions.selection === '1h' ? 'active' : '')}>
          1h
        </button>
        &nbsp;
        <button id="24h" onClick={() => updateData('24h')} className={(chartOptions.selection === '24h' ? 'active' : '')}>
          24h
        </button>
      </div>

      <Chart
        options={testOptions}
        series={chartSeries.series}
        type='area'
        width='100%'
        height='400'
      />
    </div>
  )
}

export default TimeSeriesChart;
