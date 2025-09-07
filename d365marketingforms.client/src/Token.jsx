const API_KEY = import.meta.env.VITE_MARKETING_API_KEY;
const API_URL = import.meta.env.VITE_API_URL;

export async function fetchToken() {
    const response = await fetch(`${API_URL}/token`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ apiKey: API_KEY })
    });

    if (!response.ok) {
        throw new Error('Failed to authenticate');
    }

    const data = await response.json();
    return data.token;
}