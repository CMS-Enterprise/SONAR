import React from 'react';
import { MemoryRouter } from 'react-router-dom';
import { act, render, screen } from '@testing-library/react';
import App from './App';

jest.mock('config', () => ({
  __esModule: true,
  apiUrl: 'https://localhost:9001',
  oktaAuthOptions: {
    issuer: 'https://test-mock.okta.com/oauth2/default',
    clientId: 'test-client-id',
    redirectUri: `http://localhost/login/callback`
  }
}));

test('Renders Home',
  async () => {
    await act(async () => {
      render(<MemoryRouter><App /></MemoryRouter>);
    })
    expect(screen.getByRole("navigation")).toHaveTextContent(/SONAR/);
  }
);
