import { useEffect, useState } from 'react';
import './App.css';
import { fetchToken } from './token';

const API_URL = import.meta.env.VITE_API_URL;

function App() {
    const [marketingForms, setMarketingForms] = useState();
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        fetchMarketingForms();
    }, []);

    async function fetchMarketingForms() {
        try {
            setLoading(true);

            // Use the shared fetchToken function
            const token = await fetchToken();

            const formsResponse = await fetch(`${API_URL}/marketingforms`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (!formsResponse.ok) {
                throw new Error('Failed to fetch marketing forms');
            }

            const data = await formsResponse.json();
            setMarketingForms(data);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    if (loading) {
        return <div className="loading">Loading marketing forms...</div>;
    }

    if (error) {
        return <div className="error">Error: {error}</div>;
    }

    return (
        <div className="marketing-forms-container">
            <h1>Marketing Forms</h1>
            <p>Click on a form to view its content</p>

            {marketingForms && marketingForms.length > 0 ? (
                <ul className="form-list">
                    {marketingForms.map(form => (
                        <li key={form.name} className="form-item">
                            <a
                                href={`/form/${form.slug}`}
                                className="form-link"
                            >
                                {form.name}
                            </a>
                        </li>
                    ))}
                </ul>
            ) : (
                <p>No marketing forms available</p>
            )}
        </div>
    );
}

export default App;