import React from 'react';

/**
 * Loading Spinner Component
 * Displays a centered loading spinner with optional message
 */
const LoadingSpinner = ({ size = 'medium', message = 'Loading...' }) => {
  const spinnerClass = size === 'small' ? 'spinner spinner-small' : 'spinner';

  return (
    <div style={{ textAlign: 'center', padding: '20px' }}>
      <div className={spinnerClass}></div>
      {message && (
        <p style={{ marginTop: '10px', color: '#666', fontSize: '14px' }}>
          {message}
        </p>
      )}
    </div>
  );
};

export default LoadingSpinner;
