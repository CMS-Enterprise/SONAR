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
      <nav>
        <ul>
          <li>
            <Link to="/">Home</Link>
          </li>
          <li>
            <Link to="/environments">Environments</Link>
          </li>
          <li>
            <Link to="/services">Services</Link>
          </li>
        </ul>
      </nav>
      <aside>
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
      </aside>
    </header>
  )
}

export default Header;
