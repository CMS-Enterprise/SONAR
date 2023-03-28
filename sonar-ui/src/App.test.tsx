import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';

test('Renders Home', () => {
  render(<App/>);
  const homeTitleElement = screen.getByTestId('home-title');
  expect(homeTitleElement).toBeInTheDocument();
});
