import React, { ChangeEvent } from 'react';
import { Link } from 'react-router-dom';
import Toggle from 'react-toggle';
import DarkIcon from 'components/Icons/DarkIcon';
import LightIcon from 'components/Icons/LightIcon';

import 'react-toggle/style.css';
import { ToggleIconStyle } from './Header.Style';

import classes from './Header.module.css';

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
      <nav className={classes.navBar}>
        <a href="/" className={classes.siteTitle}>SONAR</a>
        <div className={classes.navBarRightSide}>
          <a href="/" className={classes.navLink}>HOME</a>
          <label>
            <Toggle
              defaultChecked={props.enableDarkTheme}
              className={classes.themeToggle}
              aria-label="Enable the Dark Theme"
              icons={{
                checked: <LightIcon style={ToggleIconStyle} />,
                unchecked: <DarkIcon style={ToggleIconStyle} />
              }}
              onChange={handleThemeChange} />
          </label>
        </div>
      </nav>
    </header>
  )
}

export default Header;
