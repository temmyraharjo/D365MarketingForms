import { createContext, useContext, useState, useEffect } from 'react';

const TokenContext = createContext();

export function TokenProvider({ children }) {
    const [token, setToken] = useState(() => localStorage.getItem('marketing_token'));

    useEffect(() => {
        if (token) {
            localStorage.setItem('marketing_token', token);
        }
    }, [token]);

    return (
        <TokenContext.Provider value={{ token, setToken }}>
            {children}
        </TokenContext.Provider>
    );
}

export function useToken() {
    return useContext(TokenContext);
}