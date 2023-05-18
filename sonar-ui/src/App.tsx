import { ThemeProvider } from '@emotion/react';
import React, { useState } from 'react';
import {
  BrowserRouter as Router, Routes, Route
} from 'react-router-dom';
import { mainStyle } from './App.Style';

import Header from './components/App/Header';
import Environments from './pages/Environments';
import Services from './pages/Services';
import { QueryClient, QueryClientProvider } from 'react-query';

import { LightTheme, DarkTheme } from './themes';

const queryClient = new QueryClient();

function App() {
  const [enableDarkTheme, setEnableDarkTheme] = useState(false);
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={enableDarkTheme ? DarkTheme : LightTheme}>
      <Router>
        <main css={mainStyle}>
          <Header enableDarkTheme={enableDarkTheme} setEnableDarkTheme={setEnableDarkTheme} />
          <div>
            <Routes>
              <Route path="/" element={<Environments />} />
              <Route path="/services" element={<Services />} />
            </Routes>
          </div>
        </main>
      </Router>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
