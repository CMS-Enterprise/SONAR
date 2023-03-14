import React from 'react';
import ReactDOM from 'react-dom';

import App from './App';

import '@cmsgov/design-system/dist/css/index.css';
import '@cmsgov/design-system/dist/css/core-theme.css';
import './index.css';

ReactDOM.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
  document.getElementById('root')
);
