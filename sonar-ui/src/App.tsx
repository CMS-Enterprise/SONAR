import React from 'react';
import './App.css';
import ServiceView from "pages/ServiceView";
import { BrowserRouter as Router,
  Routes,
  Route } from "react-router-dom";
import Home from "./pages/Home";
import Header from "./components/App/Header";

function App() {
  return (
    <>
      <Router>
        <Header />
        <div>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/service-view" element={<ServiceView />} />
          </Routes>
        </div>
      </Router>
    </>
  );
}

export default App;
