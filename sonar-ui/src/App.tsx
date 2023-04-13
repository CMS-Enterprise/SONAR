import React from 'react';
import {
  BrowserRouter as Router, Routes, Route
} from 'react-router-dom';

import Header from './components/App/Header';
import EnvironmentView from './pages/EnvironmentView';
import Home from './pages/Home';
import ServiceView from './pages/ServiceView';
import DetailView from './pages/DetailView';

import './App.css';

function App() {
  return (
    <>
      <Router>
        <Header />
        <div>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/service-view" element={<ServiceView />} />
            <Route path="/environment-view" element={<EnvironmentView />} />
            <Route path="/detail-view" element={<DetailView />} />
          </Routes>
        </div>
      </Router>
    </>
  );
}

export default App;
