import { OktaAuthOptions } from '@okta/okta-auth-js';

/* eslint-disable  @typescript-eslint/no-explicit-any */

export const apiUrl: string = process.env.REACT_APP_API_URL || (window as any).API_URL;

export const oktaAuthOptions: OktaAuthOptions = {
  issuer: process.env.REACT_APP_OKTA_ISSUER || (window as any).OKTA_ISSUER,
  clientId: process.env.REACT_APP_OKTA_CLIENTID || (window as any).OKTA_CLIENTID,
  redirectUri: `${window.location.origin}/login/callback`,
  scopes: [ 'openid', 'email', 'profile' ]
};
