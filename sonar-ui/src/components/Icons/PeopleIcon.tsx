import { SvgIcon } from '@cmsgov/design-system';
import { SonarIconProps } from './index';

const PersonIcon = (props: SonarIconProps) =>
  <SvgIcon
    title="icon representing a person"
    viewBox="0 0 28 33"
    {...props}>

    {/*
      people-outline.svg

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

<path strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
  d="M24.9101 5.82812C24.7212 8.44943 22.7767 10.4688 20.6562 10.4688C18.5357 10.4688
  16.5879 8.45008 16.4023 5.82812C16.2089 3.10111 18.1019 1.1875 20.6562 1.1875C23.2104
  1.1875 25.1034 3.15074 24.9101 5.82812Z" />

<path strokeWidth="2" strokeMiterlimit="10" d="M20.6565 14.5938C16.4561 14.5938 12.4168
16.6801 11.4049 20.7432C11.2708 21.2808 11.6079 21.8125 12.1603 21.8125H29.1533C29.7057
21.8125 30.0409 21.2808 29.9087 20.7432C28.8968 16.615 24.8575 14.5938 20.6565 14.5938Z" />

<path strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" d="M11.8906 6.98441C11.7397
9.07785 10.1684 10.7266 8.47455 10.7266C6.78073 10.7266 5.20678 9.0785 5.05854 6.98441C4.90449
4.80654 6.43397 3.25 8.47455 3.25C10.5151 3.25 12.0446 4.8465 11.8906 6.98441Z" />

<path strokeWidth="2" strokeMiterlimit="10" strokeLinecap="round" d="M12.2775 14.7224C11.1141
14.1893 9.83278 13.9844 8.47475 13.9844C5.12319 13.9844 1.89409 15.6505 1.0852 18.8957C0.978853
19.325 1.24827 19.7497 1.68913 19.7497H8.92592" />

</SvgIcon>;

export default PersonIcon;
