import React from 'react';
import { act, render, screen } from '@testing-library/react';
import App from './App';


test('Renders Home',
  async () => {
    await act(async () => {
      render(<App />);
    })
    expect(screen.getByRole("navigation")).toHaveTextContent(/SONAR/);
  }
);
