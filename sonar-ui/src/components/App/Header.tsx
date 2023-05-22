import React, { ChangeEvent } from 'react';
import { Link } from 'react-router-dom';
import Toggle from 'react-toggle';
import DarkIcon from 'components/Icons/DarkIcon';
import LightIcon from 'components/Icons/LightIcon';

import 'react-toggle/style.css';
import { staticTextFontStyle } from '../../App.Style'
import * as styles from './Header.Style';

export type HeaderProps = {
  enableDarkTheme: boolean,
  setEnableDarkTheme: (value: boolean) => void
}

const Header = (props: HeaderProps) => {
  function handleThemeChange(e: ChangeEvent<HTMLInputElement>) {
    props.setEnableDarkTheme(e.target.checked);
  }

  return (
    <header>
      <nav css={styles.NavBarStyle}>
        <Link to="/" css={[staticTextFontStyle,styles.SiteTitleStyle]}>SONAR</Link>
        <div css={styles.NavBarRightSideStyle}>
          <Link to="/" css={[staticTextFontStyle,styles.NavLinkStyle]}>HOME</Link>
          <label>
            <Toggle
              defaultChecked={props.enableDarkTheme}
              css={styles.ThemeToggleStyle}
              aria-label="Enable the Dark Theme"
              icons={{
                checked: <LightIcon style={styles.ToggleIconStyle} />,
                unchecked: <DarkIcon style={styles.ToggleIconStyle} />
              }}
              onChange={handleThemeChange} />
          </label>
        </div>
      </nav>
    </header>
  )
}

export default Header;
