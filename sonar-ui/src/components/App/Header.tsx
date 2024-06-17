import React, { ChangeEvent } from 'react';
import { Link } from 'react-router-dom';
import Toggle from 'react-toggle';
import DarkIcon from 'components/Icons/DarkIcon';
import LightIcon from 'components/Icons/LightIcon';
import 'react-toggle/style.css';
import HelpCircleIcon from '../Icons/HelpCircleIcon';
import { FaqButtonStyle, FaqLinkStyle } from './Header.Style';
import * as styles from './Header.Style';
import LoginButton from './LoginButton';

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
        <Link
          to="/"
          css={styles.SiteTitleStyle}
          data-test="navbar-sonar-link">
          SONAR
        </Link>
        <div css={styles.NavBarRightSideStyle}>
          <LoginButton />
          <Link to="/faq" css={FaqLinkStyle}>
            <HelpCircleIcon css={FaqButtonStyle} />
          </Link>
          <label css={styles.GetThemeToggleLabelStyle} className="ds-u-focus-within" data-test="navbar-toggle-section">
            <span> Light </span>
            <Toggle
              defaultChecked={props.enableDarkTheme}
              css={styles.ThemeToggleStyle}
              aria-label="Toggle between the Light and Dark Theme"
              icons={{
                checked: <LightIcon style={styles.ToggleIconStyle} />,
                unchecked: <DarkIcon style={styles.ToggleIconStyle} />
              }}
              onChange={handleThemeChange}
            />
            <span> Dark </span>
          </label>
        </div>
      </nav>
    </header>
  )
}

export default Header;
