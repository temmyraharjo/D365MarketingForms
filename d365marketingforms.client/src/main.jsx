import ReactDOM from 'react-dom/client'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import App from './App.jsx'
import FormView from './FormView.jsx'
import './index.css'
import { TokenProvider } from './TokenContext.jsx';
import { ThemeProvider } from './ThemeContext';
import { useState, useEffect } from 'react';

function Root() {
  // Read theme from localStorage on first load
  const [theme, setTheme] = useState(() => localStorage.getItem('theme') || 'light');
  const toggleTheme = () => {
    const newTheme = theme === 'light' ? 'dark' : 'light';
    setTheme(newTheme);
    localStorage.setItem('theme', newTheme);
    window.location.reload(); // Reload to apply theme changes
  };

  // Apply theme class to body
  useEffect(() => {
    document.body.classList.remove('light-theme', 'dark-theme');
    document.body.classList.add(`${theme}-theme`);
  }, [theme]);

  return (
    <ThemeProvider value={{ theme, toggleTheme }}>
      <button onClick={toggleTheme} className="theme-toggle-btn">
        Switch to {theme === 'light' ? 'Dark' : 'Light'} Theme
      </button>
      <TokenProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/" element={<App />} />
            <Route path="/form/:slug" element={<FormView />} />
          </Routes>
        </BrowserRouter>
      </TokenProvider>
    </ThemeProvider>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<Root />);