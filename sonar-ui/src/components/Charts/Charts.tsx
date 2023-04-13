import React from 'react';
import Chart from 'react-apexcharts';


const ChartView = () => {
  //Data
  const data =
    {
      'healthCheckSamples': {
        'health-check-1': [
          ['2023-04-13T03:45:24.836Z', 0],
          ['2023-04-13T03:45:29.836Z', 1],
          ['2023-04-13T03:45:34.836Z', 2],
          ['2023-04-13T03:45:39.836Z', 3],
          ['2023-04-13T03:45:44.836Z', 4],
          ['2023-04-13T03:45:49.836Z', 5]
        ],
        'health-check-2': [
          ['2023-04-13T03:45:24.836Z', 0],
          ['2023-04-13T03:45:29.836Z', 0.1],
          ['2023-04-13T03:45:34.836Z', 0.2],
          ['2023-04-13T03:45:39.836Z', 0.3],
          ['2023-04-13T03:45:44.836Z', 0.4],
          ['2023-04-13T03:45:49.836Z', 0.5]
        ]
      },
      'totalHealthChecks': 2,
      'totalSamples': 12
    }

  //Code
  const chartOptions = {
    options:{
      chart: {
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
      xaxis: {
        type: 'datetime'
      }
    },


    selection: '1m'
  };


  const updateData = (timeline:string) => {
    chartOptions.selection = timeline;

    switch (timeline) {
      case '30s':
        ApexCharts.exec(
          'area-datetime',
          'zoomX',
          new Date('28 Jan 2013').getTime(),
          new Date('27 Feb 2013').getTime()
        )
        break
      case '1m':
        ApexCharts.exec(
          'area-datetime',
          'zoomX',
          new Date('27 Sep 2012').getTime(),
          new Date('27 Feb 2013').getTime()
        )
        break
      case '5m':
        ApexCharts.exec(
          'area-datetime',
          'zoomX',
          new Date('27 Feb 2012').getTime(),
          new Date('27 Feb 2013').getTime()
        )
        break
      case '30m':
        ApexCharts.exec(
          'area-datetime',
          'zoomX',
          new Date('01 Jan 2013').getTime(),
          new Date('27 Feb 2013').getTime()
        )
        break
      case '1h':
        ApexCharts.exec(
          'area-datetime',
          'zoomX',
          new Date('23 Jan 2012').getTime(),
          new Date('27 Feb 2013').getTime()
        )
        break
      default:
    }
  }


  const chartSeries = [
    {
      name: "Count",
      data: [1, 2, 3, 4]
    }
  ];

  return (
    <div>
      <div>
        <Chart
          options = {chartOptions.options}
          series={chartSeries}
          type='area'
          width='80%'
          height='400'
        />
      </div>

      <div className="toolbar">
        <button id="30s" onClick={() => updateData('30s')} className={(chartOptions.selection === '30s' ? 'active' : '')}>
          1M
        </button>
        &nbsp;
        <button id="1m" onClick={() => updateData('1m')} className={(chartOptions.selection === '1m' ? 'active' : '')}>
          6M
        </button>
        &nbsp;
        <button id="5m" onClick={() => updateData('5m')} className={(chartOptions.selection === '5m' ? 'active' : '')}>
          1Y
        </button>
        &nbsp;
        <button id="30m" onClick={() => updateData('30m')} className={(chartOptions.selection === '30m' ? 'active' : '')}>
          YTD
        </button>
        &nbsp;
        <button id="1h" onClick={() => updateData('1h')} className={(chartOptions.selection === '1h' ? 'active' : '')}>
          ALL
        </button>
      </div>
    </div>


  )
}

export default ChartView;
