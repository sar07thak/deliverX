import React from 'react';

/**
 * Error Message Component
 * Displays error messages in a styled alert box
 */
const ErrorMessage = ({ message, onClose }) => {
  if (!message) return null;

  return (
    <div className="alert alert-error" style={{ position: 'relative' }}>
      {message}
      {onClose && (
        <button
          onClick={onClose}
          style={{
            position: 'absolute',
            right: '12px',
            top: '50%',
            transform: 'translateY(-50%)',
            background: 'none',
            border: 'none',
            fontSize: '20px',
            cursor: 'pointer',
            color: '#721c24',
            padding: '0 5px'
          }}
        >
          ×
        </button>
      )}
    </div>
  );
};

export default ErrorMessage;
