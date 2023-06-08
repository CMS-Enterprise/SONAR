import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';

test('Renders Home', () => {
  render(<App/>);
  expect(screen.getByRole("navigation")).toHaveTextContent(/SONAR/);

});
