# DeliverX - Delivery Partner Registration & KYC Portal

A complete React-based web application for delivery partner onboarding and KYC verification.

## Features

- **OTP-based Authentication** - Secure phone number verification
- **Multi-step Registration** - Comprehensive delivery partner profile creation
- **KYC Verification** - Aadhaar, PAN, and Bank account verification
- **Dashboard** - Real-time KYC status tracking
- **Responsive Design** - Works on desktop and mobile devices

## Tech Stack

- **React 18** - Modern React with Hooks
- **React Router v6** - Client-side routing
- **Axios** - HTTP client with interceptors
- **Context API** - State management
- **CSS** - Clean, responsive styling

## Project Structure

```
deliverx-ui/
├── public/
│   └── index.html              # HTML entry point
├── src/
│   ├── components/             # Reusable components
│   │   ├── ErrorMessage.js     # Error display component
│   │   ├── LoadingSpinner.js   # Loading indicator
│   │   ├── Navbar.js           # Navigation bar
│   │   ├── ProtectedRoute.js   # Auth guard for routes
│   │   └── StepIndicator.js    # Multi-step progress indicator
│   ├── context/
│   │   └── AuthContext.js      # Authentication context provider
│   ├── pages/                  # Page components
│   │   ├── LoginPage.js        # OTP login
│   │   ├── RegistrationPage.js # Multi-step registration wizard
│   │   ├── DashboardPage.js    # Main dashboard
│   │   ├── ProfilePage.js      # User profile
│   │   ├── KYCPage.js          # KYC hub
│   │   ├── AadhaarVerificationPage.js  # Aadhaar verification
│   │   ├── PANVerificationPage.js      # PAN verification
│   │   └── BankVerificationPage.js     # Bank verification
│   ├── services/               # API service layer
│   │   ├── api.js              # Axios client with interceptors
│   │   ├── authService.js      # Authentication APIs
│   │   ├── registrationService.js  # Registration APIs
│   │   └── kycService.js       # KYC verification APIs
│   ├── App.js                  # Main app with routing
│   ├── index.js                # React entry point
│   └── index.css               # Global styles
├── package.json
└── README.md
```

## Getting Started

### Prerequisites

- Node.js (v14 or higher)
- npm or yarn
- Backend API running on http://localhost:5205

### Installation

Dependencies are already installed. If you need to reinstall:

```bash
npm install
```

### Running the Application

1. **Ensure Backend is Running**
   - Backend API should be running on http://localhost:5205
   - All 8 KYC endpoints should be functional

2. **Start the React App**
   ```bash
   npm start
   ```

3. **Access the Application**
   - Open http://localhost:3000 in your browser
   - You'll be redirected to the login page

## User Flow

### 1. Login (OTP Authentication)
- Enter 10-digit phone number
- Receive and enter 6-digit OTP
- Auto-login on successful verification

### 2. Registration (if new user)
**Step 1: Personal Information**
- Full name, DOB, gender, email
- Complete address details

**Step 2: Service Details**
- Vehicle type selection
- Languages known
- Availability and service area
- Pricing configuration

**Step 3: Review & Submit**
- Review all entered information
- Submit to complete registration

### 3. KYC Verification
**Aadhaar Verification**
- DigiLocker integration (simulated in MVP)
- Automatic data extraction
- Instant verification

**PAN Verification**
- Enter 10-character PAN
- Government database verification
- Name matching with Aadhaar

**Bank Verification**
- Enter account number, IFSC, holder name
- Penny drop verification
- Bank details confirmation

### 4. Dashboard
- View overall KYC status
- Track individual verification progress
- Access profile and complete pending verifications
- Start delivering when fully verified

## API Integration

### Base URL
All API calls are made to: `http://localhost:5205`

### Authentication
- JWT token stored in localStorage
- Auto-attached to all requests via Axios interceptor
- Auto-redirect to login on 401 errors

### Available Endpoints

**Authentication**
- POST `/api/auth/send-otp` - Send OTP
- POST `/api/auth/verify-otp` - Verify OTP

**Registration**
- POST `/api/registration/initiate` - Start registration
- POST `/api/registration/complete-profile` - Complete profile

**KYC**
- POST `/api/kyc/aadhaar/initiate` - Start Aadhaar verification
- POST `/api/kyc/aadhaar/complete` - Complete Aadhaar verification
- POST `/api/kyc/pan/verify` - Verify PAN
- POST `/api/kyc/bank/verify` - Verify bank account
- GET `/api/kyc/status/:userId` - Get KYC status

## Features & Implementation

### State Management
- **AuthContext** - Global authentication state
- **localStorage** - Token and user persistence
- **React Hooks** - Local component state

### Form Validation
- Client-side validation for all forms
- Format validation (phone, PAN, IFSC, etc.)
- Required field checks
- Real-time error messages

### Error Handling
- User-friendly error messages
- API error interception
- Network error handling
- Loading states for all async operations

### Responsive Design
- Mobile-first approach
- Flexible grid layouts
- Responsive navigation
- Touch-friendly buttons

## Component Details

### Reusable Components

**LoadingSpinner**
- Displays during async operations
- Customizable size and message
- Centered layout

**ErrorMessage**
- Styled error alerts
- Dismissible with close button
- Supports custom messages

**Navbar**
- Responsive navigation
- Dynamic user info display
- Logout functionality
- Hidden when not authenticated

**ProtectedRoute**
- Route-level authentication guard
- Auto-redirect to login
- Loading state during auth check

**StepIndicator**
- Visual progress tracker
- Multi-step form support
- Active step highlighting

## Styling

- **No external UI libraries** - Pure CSS implementation
- **CSS Variables** - Consistent color scheme
- **Flexbox/Grid** - Modern layouts
- **Transitions** - Smooth interactions
- **Mobile Responsive** - Works on all screen sizes

## Security

- JWT token authentication
- Secure token storage
- Auto-logout on token expiry
- Protected routes
- HTTPS-ready (when deployed)

## Known Limitations (MVP)

1. **DigiLocker Integration** - Simulated for MVP (no actual redirect)
2. **Real-time Updates** - Manual refresh needed for status updates
3. **File Uploads** - Not implemented in MVP
4. **Email Verification** - Not implemented
5. **Password Recovery** - OTP-based auth only

## Future Enhancements

- Real DigiLocker integration
- Document upload functionality
- Real-time WebSocket updates
- Email notifications
- Advanced analytics dashboard
- Multi-language support
- Dark mode theme

## Troubleshooting

### App won't start
```bash
# Clear node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
npm start
```

### API connection errors
- Ensure backend is running on http://localhost:5205
- Check CORS configuration on backend
- Verify API endpoints are accessible

### Authentication issues
- Clear localStorage: `localStorage.clear()`
- Check token expiry
- Verify OTP service is working

### Build errors
```bash
# Clean build
rm -rf build
npm run build
```

## Build for Production

```bash
npm run build
```

This creates an optimized production build in the `build/` folder.

## Available Scripts

- `npm start` - Start development server
- `npm run build` - Build for production
- `npm test` - Run tests
- `npm run eject` - Eject from Create React App (one-way operation)

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)
- Mobile browsers

## Contributing

This is an MVP. Focus areas for contribution:
1. Add unit tests
2. Improve error handling
3. Add loading skeletons
4. Implement real DigiLocker
5. Add more validation

## License

Proprietary - DeliverX Platform

## Support

For issues or questions, contact the development team.

---

**Built with ❤️ for DeliverX Delivery Partners**
