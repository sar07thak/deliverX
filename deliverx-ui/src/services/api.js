import axios from 'axios';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: 'http://localhost:5205',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - Add authorization token to requests
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor - Handle errors globally
api.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    console.log('API Error Details:', error.response);

    if (error.response) {
      // Server responded with error status
      let message = 'An error occurred';
      const responseData = error.response.data;

      // Try different ways to extract error message
      if (typeof responseData === 'string') {
        // Sometimes the error is a plain string
        message = responseData;

        // Check for specific error patterns in the string
        if (message.includes('UNIQUE constraint failed: Users.Email') || message.includes('Email')) {
          message = 'This email address is already registered. Please use a different email.';
        }
      } else if (responseData) {
        // Object response - try different properties
        message = responseData.message
          || responseData.error
          || responseData.detail
          || 'An error occurred';

        // Handle ASP.NET Core validation problem details format
        if (responseData.errors && typeof responseData.errors === 'object') {
          const errorMessages = Object.entries(responseData.errors)
            .map(([field, messages]) => {
              const msgArray = Array.isArray(messages) ? messages : [messages];
              return `${field}: ${msgArray.join(', ')}`;
            })
            .join('; ');
          if (errorMessages) {
            message = errorMessages;
          }
        }

        // If still no message, use title as fallback
        if (message === 'An error occurred' && responseData.title) {
          message = responseData.title;
        }

        // Check if it's a database constraint error
        const dataString = JSON.stringify(responseData);
        if (dataString.includes('UNIQUE constraint failed: Users.Email') || dataString.includes('Email')) {
          message = 'This email address is already registered. Please use a different email.';
        } else if (dataString.includes('UNIQUE constraint')) {
          message = 'This information is already registered. Please use different details.';
        }
      }

      // Handle unauthorized errors
      if (error.response.status === 401) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        window.location.href = '/login';
      }

      // Create enhanced error object
      const enhancedError = new Error(message);
      enhancedError.response = error.response;
      enhancedError.status = error.response.status;

      return Promise.reject(enhancedError);
    } else if (error.request) {
      // Request made but no response received
      return Promise.reject(new Error('No response from server. Please check your connection.'));
    } else {
      // Something else happened
      return Promise.reject(new Error(error.message || 'An unexpected error occurred'));
    }
  }
);

export default api;
