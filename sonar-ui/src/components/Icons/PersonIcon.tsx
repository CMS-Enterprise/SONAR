import { SvgIcon } from '@cmsgov/design-system';
import { SonarIconProps } from './index';

const PersonIcon = (props: SonarIconProps) =>
  <SvgIcon
    title="icon representing a person"
    viewBox="0 0 28 28"
    {...props}>

    {/*
      person-cicle-outline.svg

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
      THE SOFTWARE.
    */}

    <path d="M13.68 0.09C6.14 -0.00 -0.00 6.14 0.09 13.68C0.19 20.91 6.08 26.80 13.31 26.90C20.85 27.00 27.00
    20.85 26.90 13.31C26.80 6.08 20.91 0.19 13.68 0.09ZM21.83 21.18C21.80 21.21 21.77 21.23 21.74 21.24C21.70
    21.26 21.67 21.26 21.63 21.26C21.59 21.26 21.55 21.25 21.52 21.23C21.49 21.21 21.46 21.19 21.43 21.16C20.86
    20.41 20.15 19.76 19.35 19.25C17.71 18.20 15.63 17.62 13.5 17.62C11.36 17.62 9.28 18.20 7.64 19.25C6.84
    19.76 6.13 20.41 5.56 21.16C5.53 21.19 5.50 21.21 5.47 21.23C5.44 21.25 5.40 21.26 5.36 21.26C5.32 21.26
    5.29 21.26 5.25 21.24C5.22 21.23 5.19 21.21 5.16 21.18C3.27 19.14 2.20 16.47 2.15 13.69C2.05 7.42 7.19 2.17
    13.47 2.15C19.74 2.14 24.84 7.23 24.84 13.49C24.84 16.34 23.77 19.09 21.83 21.18Z"/>

    <path d="M13.5 6.28C12.22 6.28 11.07 6.75 10.26 7.62C9.44 8.48 9.03 9.68 9.13 10.96C9.31 13.49 11.27 15.56
    13.5 15.56C15.72 15.56 17.67 13.49 17.86 10.97C17.96 9.69 17.55 8.51 16.72 7.63C15.90 6.76 14.76 6.28 13.5
    6.28Z"/>

</SvgIcon>;

export default PersonIcon;
