import React from 'react';
import { MemoryRouter } from 'react-router-dom';
import { act, render, screen } from '@testing-library/react';
import App from './App';


test('Renders Home',
  async () => {
    await act(async () => {
      render(<MemoryRouter><App /></MemoryRouter>);
    })
    expect(screen.getByRole("navigation")).toHaveTextContent(/SONAR/);
  }
);
