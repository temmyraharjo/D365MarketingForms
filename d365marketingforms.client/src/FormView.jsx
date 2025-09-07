import { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import './App.css';
import { fetchToken } from './token';

const API_URL = import.meta.env.VITE_API_URL;

function FormView() {
    const { slug } = useParams();
    const navigate = useNavigate();
    const [form, setForm] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const iframeRef = useRef(null);

    useEffect(() => {
        fetchFormBySlug();
    }, [slug]);

    async function fetchFormBySlug() {
        try {
            setLoading(true);
            const token = await fetchToken();
            const formResponse = await fetch(`${API_URL}/marketingforms/${slug}`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            if (!formResponse.ok) {
                if (formResponse.status === 404) {
                    throw new Error('Form not found');
                }
                throw new Error('Failed to fetch form data');
            }
            const data = await formResponse.json();
            setForm(data);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    // Inject the HTML into the iframe after form is loaded and auto-resize
    useEffect(() => {
        if (form && form.htmlContent && iframeRef.current) {
            const iframe = iframeRef.current;
            const doc = iframe.contentDocument || iframe.contentWindow.document;
            doc.open();
            doc.write(form.htmlContent);
            doc.close();

            // Auto-resize after content loads
            const resizeIframe = () => {
                if (iframe && iframe.contentWindow && iframe.contentDocument) {
                    const body = iframe.contentDocument.body;
                    const html = iframe.contentDocument.documentElement;
                    // Get the max height and width of body and html, then add 200px
                    const height = Math.max(
                        body.scrollHeight,
                        body.offsetHeight,
                        html.clientHeight,
                        html.scrollHeight,
                        html.offsetHeight
                    ) + 20;
                    const width = Math.max(
                        body.scrollWidth,
                        body.offsetWidth,
                        html.clientWidth,
                        html.scrollWidth,
                        html.offsetWidth
                    ) + 20;
                    iframe.style.height = height + 'px';
                    iframe.style.width = width + 'px';
                }
            };

            // Wait for content to load, then resize
            iframe.onload = resizeIframe;
            // Also try to resize after a short delay in case onload doesn't fire
            setTimeout(resizeIframe, 500);
        }
    }, [form]);

    if (loading) {
        return <div className="loading">Loading form...</div>;
    }

    if (error) {
        return (
            <div className="error-container">
                <div className="error">Error: {error}</div>
                <button onClick={() => navigate('/')} className="back-button">
                    Back to Forms List
                </button>
            </div>
        );
    }

    return (
        <div className="form-view-container">
            <button onClick={() => navigate('/')} className="back-button">
                Back to Forms List
            </button>
            <h1>{form.name}</h1>
            <div className="form-content" style={{padding: 0}}>
                <iframe
                    ref={iframeRef}
                    title="Marketing Form"
                    style={{
                        width: '100%',
                        minWidth: '200px',
                        minHeight: '400px',
                        border: 'none',
                        background: 'transparent'
                    }}
                />
            </div>
        </div>
    );
}

export default FormView;