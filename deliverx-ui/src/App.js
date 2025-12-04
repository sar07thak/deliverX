import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';

// Components
import Navbar from './components/Navbar';
import ProtectedRoute from './components/ProtectedRoute';

// Pages
import LoginPage from './pages/LoginPage';
import RegistrationPage from './pages/RegistrationPage';
import DashboardPage from './pages/DashboardPage';
import ProfilePage from './pages/ProfilePage';
import KYCPage from './pages/KYCPage';
import AadhaarVerificationPage from './pages/AadhaarVerificationPage';
import PANVerificationPage from './pages/PANVerificationPage';
import BankVerificationPage from './pages/BankVerificationPage';

// Delivery Pages
import DeliveryListPage from './pages/DeliveryListPage';
import CreateDeliveryPage from './pages/CreateDeliveryPage';
import DeliveryTrackingPage from './pages/DeliveryTrackingPage';
import AvailableJobsPage from './pages/AvailableJobsPage';
import ServiceAreaPage from './pages/ServiceAreaPage';

// Feature Pages (F-07 to F-12)
import RatingsPage from './pages/RatingsPage';
import ComplaintsPage from './pages/ComplaintsPage';
import NewComplaintPage from './pages/NewComplaintPage';
import ComplaintDetailPage from './pages/ComplaintDetailPage';
import WalletPage from './pages/WalletPage';
import SubscriptionsPage from './pages/SubscriptionsPage';
import ReferralsPage from './pages/ReferralsPage';
import AdminDashboardPage from './pages/AdminDashboardPage';
import DPCMDashboardPage from './pages/DPCMDashboardPage';

/**
 * Main App Component
 * Sets up routing and authentication context
 */
function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="App">
          <Navbar />

          <Routes>
            {/* Public Routes */}
            <Route path="/login" element={<LoginPage />} />

            {/* Protected Routes */}
            <Route
              path="/register"
              element={
                <ProtectedRoute>
                  <RegistrationPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/dashboard"
              element={
                <ProtectedRoute>
                  <DashboardPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/profile"
              element={
                <ProtectedRoute>
                  <ProfilePage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/kyc"
              element={
                <ProtectedRoute>
                  <KYCPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/kyc/aadhaar"
              element={
                <ProtectedRoute>
                  <AadhaarVerificationPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/kyc/pan"
              element={
                <ProtectedRoute>
                  <PANVerificationPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/kyc/bank"
              element={
                <ProtectedRoute>
                  <BankVerificationPage />
                </ProtectedRoute>
              }
            />

            {/* Delivery Routes */}
            <Route
              path="/deliveries"
              element={
                <ProtectedRoute>
                  <DeliveryListPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/deliveries/create"
              element={
                <ProtectedRoute>
                  <CreateDeliveryPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/deliveries/pending"
              element={
                <ProtectedRoute>
                  <AvailableJobsPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/deliveries/:id"
              element={
                <ProtectedRoute>
                  <DeliveryTrackingPage />
                </ProtectedRoute>
              }
            />

            {/* Service Area Route */}
            <Route
              path="/service-area"
              element={
                <ProtectedRoute>
                  <ServiceAreaPage />
                </ProtectedRoute>
              }
            />

            {/* Ratings Route (F-07) */}
            <Route
              path="/ratings"
              element={
                <ProtectedRoute>
                  <RatingsPage />
                </ProtectedRoute>
              }
            />

            {/* Complaints Routes (F-08) */}
            <Route
              path="/complaints"
              element={
                <ProtectedRoute>
                  <ComplaintsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/complaints/new"
              element={
                <ProtectedRoute>
                  <NewComplaintPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/complaints/:id"
              element={
                <ProtectedRoute>
                  <ComplaintDetailPage />
                </ProtectedRoute>
              }
            />

            {/* Wallet Route (F-09) */}
            <Route
              path="/wallet"
              element={
                <ProtectedRoute>
                  <WalletPage />
                </ProtectedRoute>
              }
            />

            {/* Subscriptions Route (F-10) */}
            <Route
              path="/subscriptions"
              element={
                <ProtectedRoute>
                  <SubscriptionsPage />
                </ProtectedRoute>
              }
            />

            {/* Referrals Route (F-11) */}
            <Route
              path="/referrals"
              element={
                <ProtectedRoute>
                  <ReferralsPage />
                </ProtectedRoute>
              }
            />

            {/* Admin Dashboard Route (F-12) - SuperAdmin Only */}
            <Route
              path="/admin"
              element={
                <ProtectedRoute>
                  <AdminDashboardPage />
                </ProtectedRoute>
              }
            />

            {/* DPCM Dashboard Route - Channel Manager Only */}
            <Route
              path="/dpcm"
              element={
                <ProtectedRoute>
                  <DPCMDashboardPage />
                </ProtectedRoute>
              }
            />

            {/* Default Route - Redirect to login */}
            <Route path="/" element={<Navigate to="/login" replace />} />

            {/* 404 Route */}
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
