import React from 'react';
import {
  BrowserRouter as Router, Routes, Route
} from 'react-router-dom';

import Header from './components/App/Header';
import EnvironmentView from './pages/EnvironmentView';
import Home from './pages/Home';
import ServiceView from './pages/ServiceView';
import { QueryClient, QueryClientProvider } from "react-query";
import './App.css';

const queryClient = new QueryClient();

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <Header />
        <div>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/service-view" element={<ServiceView />} />
            <Route path="/environment-view" element={<EnvironmentView />} />
          </Routes>
        </div>
      </Router>
    </QueryClientProvider>
  );
}

export default App;
