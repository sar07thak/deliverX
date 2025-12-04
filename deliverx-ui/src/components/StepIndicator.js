import React from 'react';

/**
 * Step Indicator Component
 * Shows progress in multi-step forms
 */
const StepIndicator = ({ currentStep, totalSteps, steps }) => {
  return (
    <div style={styles.container}>
      <div style={styles.progressBar}>
        <div
          style={{
            ...styles.progressFill,
            width: `${(currentStep / totalSteps) * 100}%`
          }}
        />
      </div>

      <div style={styles.steps}>
        {steps.map((step, index) => (
          <div key={index} style={styles.step}>
            <div
              style={{
                ...styles.stepCircle,
                backgroundColor: index + 1 <= currentStep ? '#4CAF50' : '#e0e0e0',
                color: index + 1 <= currentStep ? 'white' : '#666'
              }}
            >
              {index + 1}
            </div>
            <div
              style={{
                ...styles.stepLabel,
                color: index + 1 === currentStep ? '#4CAF50' : '#666',
                fontWeight: index + 1 === currentStep ? 'bold' : 'normal'
              }}
            >
              {step}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

const styles = {
  container: {
    marginBottom: '30px'
  },
  progressBar: {
    width: '100%',
    height: '8px',
    backgroundColor: '#e0e0e0',
    borderRadius: '4px',
    overflow: 'hidden',
    marginBottom: '20px'
  },
  progressFill: {
    height: '100%',
    backgroundColor: '#4CAF50',
    transition: 'width 0.3s ease'
  },
  steps: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start'
  },
  step: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    flex: 1
  },
  stepCircle: {
    width: '36px',
    height: '36px',
    borderRadius: '50%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '14px',
    fontWeight: 'bold',
    marginBottom: '8px',
    transition: 'all 0.3s ease'
  },
  stepLabel: {
    fontSize: '12px',
    textAlign: 'center',
    maxWidth: '120px'
  }
};

export default StepIndicator;
