import React from 'react';
import Chart from 'react-apexcharts';


const ChartView = () => {
  //Code
  const chartOptions = {
    chart: {
      background: "#f4f4f4f4",
      forecolor: "#333"
    },
    xaxis: {
      categories: ["a", "b", "c", "d"]

    },
    dataLabels: {
      enabled: false
    },
    plotOptions: {
      bar: {
        horizontal: false
      }

    },
  };
  const chartSeries = [
    {
      name: "Count",
      data: [1, 2, 3, 4]
    }
  ];

  return (
    <div>
      <Chart
        options = {chartOptions}
        series={chartSeries}
        type='bar'
        width='80%'
        height='400'
      />
    </div>
  )
}

export default ChartView;
