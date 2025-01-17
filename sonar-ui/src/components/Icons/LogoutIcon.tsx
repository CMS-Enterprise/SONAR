import { SvgIcon } from '@cmsgov/design-system';
import { SonarIconProps } from './index';

const LogoutIcon = (props: SonarIconProps) =>
  <SvgIcon
    title="icon representing logout"
    viewBox="0 0 28 28"
    {...props}>

    {/*
      log-out-outline.svg

      The MIT License (MIT)

      Copyright (c) 2015-present Ionic (http://ionic.io/)

      Permission is hereby granted, free of charge, to any person obtaining a copy
      of this software and associated documentation files (the "Software"), to deal
      in the Software without restriction, including without limitation the rights
      to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
      copies of the Software, and to permit persons to whom the Software is
      furnished to do so, subject to the following conditions:

      The above copyright notice and this permission notice shall be included in
      all copies or substantial portions of the Software.

      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
      IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
      FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
      AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
      LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
      OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
      THE SOFTWARE. */}
    <path fillRule="evenodd" clipRule="evenodd"
          d="M3.76 2.54C3.34 2.54 2.92 2.74 2.62 3.10C2.32 3.46 2.15
          3.94 2.15 4.45V23.54C2.15 24.05 2.32 24.53 2.62 24.89C2.92
          25.25 3.34 25.45 3.76 25.45H14.53C14.96 25.45 15.37 25.25 15.68
          24.89C15.98 24.53 16.15 24.05 16.15 23.54V20.36C16.15 19.66 16.63
          19.09 17.23 19.09C17.82 19.09 18.30 19.66 18.30 20.36V23.54C18.30
          24.72 17.91 25.85 17.20 26.69C16.49 27.53 15.53 28 14.53 28H3.76C2.76
          28 1.81 27.53 1.10 26.69C0.39 25.85 0 24.72 0 23.54V4.45C0 3.27
          0.39 2.14 1.10 1.30C1.81 0.46 2.76 0 3.76 0H14C15.01 0 16.04
          0.47 16.83 1.20C17.62 1.92 18.30 3.05 18.30 4.45V7.63C18.30
          8.33 17.82 8.90 17.23 8.90C16.63 8.90 16.15 8.33 16.15
          7.63V4.45C16.15 4.09 15.96 3.63 15.50 3.20C15.05 2.78 14.47
          2.54 14 2.54H3.76ZM20.77 6.73C21.19 6.23 21.87 6.23 22.3
          6.73L27.68 13.1C28.10 13.59 28.10 14.40 27.68 14.9L22.3 21.26C21.87
          21.76 21.19 21.76 20.77 21.26C20.35 20.76 20.35 19.96 20.77 19.46L24.32
          15.27H8.61C8.02 15.27 7.53 14.70 7.53 14C7.53 13.29 8.02 12.72
          8.61 12.72H24.32L20.77 8.53C20.35 8.03 20.35 7.23 20.77 6.73Z"/>
  </SvgIcon>;

export default LogoutIcon;
