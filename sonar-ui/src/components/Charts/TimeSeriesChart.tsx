import React, { useEffect, useState } from 'react';
import Chart from 'react-apexcharts';
import { ApexOptions } from "apexcharts";
import { HealthCheckType, ServiceHierarchyConfiguration } from 'api/data-contracts';

const TimeSeriesChart: React.FC<{
  svcHierarchyCfg: ServiceHierarchyConfiguration | null,
  healthCheckName:string,
  timeSeriesData:number[][]
}> = ({ svcHierarchyCfg, healthCheckName, timeSeriesData }) => {
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
      case '10m':
        ApexCharts.exec(
          healthCheckName,
          'zoomX',
          currentTime,
          currentTime + (10 * MIN)
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

  const updateYAxis = ()  => {
    svcHierarchyCfg?.services?.map((s: any) =>
      s.healthChecks.filter((hc:any) => (hc.name === healthCheckName && hc.type != HealthCheckType.HttpRequest)).map((hc:any) =>
        hc.definition.conditions.map((c:any) =>
          ApexCharts.exec(healthCheckName, "addYaxisAnnotation", {
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
          })
        )
      )
    )
    setDisplayAnnotation(true);
  };

  const clearAnnotation = () => {
    ApexCharts.exec(healthCheckName, 'clearAnnotations');
    ApexCharts.exec(healthCheckName, 'clearAnnotations');
    setDisplayAnnotation(false);
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
        <button id="10m" onClick={() => updateData('10m')}>10m</button>
        &nbsp;
        <button id="all" onClick={() => updateData('all')}>All</button>
        &nbsp;
        { displayAnnotation?
          <button id="hideAnnotation" onClick={() => clearAnnotation()}>Hide Annotations</button>:
          <button id="displayAnnotation" onClick={() => updateYAxis()}>Display Annotations</button>
        }


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


