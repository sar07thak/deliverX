import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

/**
 * Navigation Bar Component
 * Displays app navigation with role-based menu items
 */
const Navbar = () => {
  const { user, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [showMoreMenu, setShowMoreMenu] = useState(false);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  if (!isAuthenticated()) {
    return null;
  }

  const role = user?.role;
  const isSuperAdmin = role === 'SuperAdmin' || role === 'SA';
  const isDPCM = role === 'DPCM';
  const isAdmin = isSuperAdmin || isDPCM;
  const isDP = role === 'DP';
  const isBC = role === 'DBC' || role === 'BC';
  const isEC = role === 'EC';

  // Get user-friendly role display name
  const getRoleDisplayName = (roleCode) => {
    const roleNames = {
      'EC': 'Consumer',
      'DBC': 'Business',
      'BC': 'Business',
      'DP': 'Partner',
      'DPCM': 'Manager',
      'SuperAdmin': 'Admin',
      'SA': 'Admin'
    };
    return roleNames[roleCode] || roleCode;
  };

  // Get role badge color
  const getRoleBadgeColor = (roleCode) => {
    const colors = {
      'EC': '#28a745',
      'DBC': '#17a2b8',
      'BC': '#17a2b8',
      'DP': '#ffc107',
      'DPCM': '#6f42c1',
      'SuperAdmin': '#dc3545',
      'SA': '#dc3545'
    };
    return colors[roleCode] || '#667eea';
  };

  // Check if current path matches
  const isActive = (path) => {
    return location.pathname === path || location.pathname.startsWith(path + '/');
  };

  // Role-specific menu items
  const getMoreMenuItems = () => {
    const items = [];

    // Common for EC and BC
    if (isEC || isBC) {
      items.push({ to: '/complaints', label: 'Complaints', icon: '📋' });
      items.push({ to: '/referrals', label: 'Referrals', icon: '🎁' });
    }

    // BC specific
    if (isBC) {
      items.push({ to: '/subscriptions', label: 'Subscriptions', icon: '💳' });
    }

    // DP specific
    if (isDP) {
      items.push({ to: '/kyc', label: 'KYC Status', icon: '📄' });
      items.push({ to: '/ratings', label: 'My Ratings', icon: '⭐' });
      items.push({ to: '/service-area', label: 'Service Area', icon: '📍' });
    }

    // DPCM specific
    if (isDPCM) {
      items.push({ to: '/dpcm', label: 'DPCM Dashboard', icon: '📊' });
      items.push({ to: '/service-area', label: 'Service Areas', icon: '📍' });
    }

    // SuperAdmin specific
    if (isSuperAdmin) {
      items.push({ to: '/admin', label: 'Admin Dashboard', icon: '🛡️' });
      items.push({ to: '/kyc', label: 'KYC Approvals', icon: '✅' });
      items.push({ to: '/service-area', label: 'Service Areas', icon: '📍' });
      items.push({ to: '/subscriptions', label: 'Subscriptions', icon: '💳' });
      items.push({ to: '/ratings', label: 'Ratings', icon: '⭐' });
      items.push({ to: '/complaints', label: 'All Complaints', icon: '📋' });
    }

    // Profile for all
    items.push({ to: '/profile', label: 'Profile', icon: '👤' });

    return items;
  };

  // Get main dashboard link based on role
  const getDashboardLink = () => {
    if (isSuperAdmin) return '/admin';
    if (isDPCM) return '/dpcm';
    return '/dashboard';
  };

  return (
    <nav style={styles.navbar}>
      <div style={styles.container}>
        <Link to={getDashboardLink()} style={styles.logo}>
          DeliverX
          {(isSuperAdmin || isDPCM) && (
            <span style={{ fontSize: '10px', marginLeft: '5px', opacity: 0.8 }}>
              {isSuperAdmin ? 'Admin' : 'Manager'}
            </span>
          )}
        </Link>

        <div style={styles.navLinks}>
          {/* Dashboard - different for admin roles */}
          <Link
            to={getDashboardLink()}
            style={{
              ...styles.navLink,
              backgroundColor: isActive(getDashboardLink()) ? 'rgba(255,255,255,0.15)' : 'transparent'
            }}
          >
            Dashboard
          </Link>

          {/* Deliveries - not for SuperAdmin */}
          {!isSuperAdmin && (
            <Link
              to="/deliveries"
              style={{
                ...styles.navLink,
                backgroundColor: isActive('/deliveries') ? 'rgba(255,255,255,0.15)' : 'transparent'
              }}
            >
              {isDP ? 'My Deliveries' : isDPCM ? 'All Deliveries' : 'Deliveries'}
            </Link>
          )}

          {/* Available Deliveries - DP only */}
          {isDP && (
            <Link
              to="/deliveries/pending"
              style={{
                ...styles.navLink,
                backgroundColor: isActive('/deliveries/pending') ? 'rgba(255,255,255,0.15)' : 'transparent'
              }}
            >
              Available Jobs
            </Link>
          )}

          {/* Wallet - not for SuperAdmin */}
          {!isSuperAdmin && (
            <Link
              to="/wallet"
              style={{
                ...styles.navLink,
                backgroundColor: isActive('/wallet') ? 'rgba(255,255,255,0.15)' : 'transparent'
              }}
            >
              Wallet
            </Link>
          )}

          {/* DPCM Quick Links */}
          {isDPCM && (
            <Link
              to="/dpcm"
              style={{
                ...styles.navLink,
                backgroundColor: isActive('/dpcm') ? 'rgba(255,255,255,0.15)' : 'transparent'
              }}
            >
              My Partners
            </Link>
          )}

          {/* Admin Quick Links */}
          {isSuperAdmin && (
            <>
              <Link
                to="/admin"
                style={{
                  ...styles.navLink,
                  backgroundColor: isActive('/admin') ? 'rgba(255,255,255,0.15)' : 'transparent'
                }}
              >
                Users
              </Link>
              <Link
                to="/kyc"
                style={{
                  ...styles.navLink,
                  backgroundColor: isActive('/kyc') ? 'rgba(255,255,255,0.15)' : 'transparent'
                }}
              >
                KYC
              </Link>
            </>
          )}

          {/* More Menu Dropdown */}
          <div style={styles.dropdown}>
            <button
              onClick={() => setShowMoreMenu(!showMoreMenu)}
              style={styles.dropdownBtn}
            >
              More ▾
            </button>
            {showMoreMenu && (
              <>
                <div
                  style={styles.dropdownOverlay}
                  onClick={() => setShowMoreMenu(false)}
                />
                <div style={styles.dropdownMenu}>
                  {getMoreMenuItems().map((item, index) => (
                    <Link
                      key={index}
                      to={item.to}
                      style={{
                        ...styles.dropdownLink,
                        backgroundColor: isActive(item.to) ? '#f0f0f0' : 'transparent'
                      }}
                      onClick={() => setShowMoreMenu(false)}
                    >
                      <span style={{ marginRight: '8px' }}>{item.icon}</span>
                      {item.label}
                    </Link>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>

        <div style={styles.userSection}>
          {user && (
            <span style={styles.userName}>
              {user.role && (
                <span style={{
                  ...styles.roleBadge,
                  backgroundColor: getRoleBadgeColor(user.role)
                }}>
                  {getRoleDisplayName(user.role)}
                </span>
              )}
              <span style={{ marginLeft: '8px' }}>{user.name || user.phone || 'User'}</span>
            </span>
          )}
          <button onClick={handleLogout} className="btn btn-danger" style={{ marginLeft: '15px', padding: '6px 12px', fontSize: '13px' }}>
            Logout
          </button>
        </div>
      </div>
    </nav>
  );
};

const styles = {
  navbar: {
    backgroundColor: '#2c3e50',
    color: 'white',
    padding: '0',
    marginBottom: '20px',
    boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
    position: 'sticky',
    top: 0,
    zIndex: 1000
  },
  container: {
    maxWidth: '1400px',
    margin: '0 auto',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '0 20px',
    height: '60px'
  },
  logo: {
    fontSize: '22px',
    fontWeight: 'bold',
    color: '#4CAF50',
    textDecoration: 'none',
    display: 'flex',
    alignItems: 'center'
  },
  navLinks: {
    display: 'flex',
    gap: '5px',
    flex: 1,
    marginLeft: '30px',
    alignItems: 'center'
  },
  navLink: {
    color: 'white',
    textDecoration: 'none',
    fontSize: '14px',
    fontWeight: '500',
    padding: '8px 12px',
    borderRadius: '4px',
    transition: 'background-color 0.2s'
  },
  dropdown: {
    position: 'relative'
  },
  dropdownBtn: {
    color: 'white',
    background: 'transparent',
    border: '1px solid rgba(255,255,255,0.3)',
    fontSize: '14px',
    fontWeight: '500',
    padding: '8px 14px',
    borderRadius: '4px',
    cursor: 'pointer',
    transition: 'all 0.2s'
  },
  dropdownOverlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    zIndex: 999
  },
  dropdownMenu: {
    position: 'absolute',
    top: '100%',
    right: 0,
    background: 'white',
    borderRadius: '8px',
    boxShadow: '0 4px 20px rgba(0,0,0,0.15)',
    minWidth: '200px',
    zIndex: 1000,
    marginTop: '5px',
    padding: '8px 0',
    maxHeight: '400px',
    overflowY: 'auto'
  },
  dropdownLink: {
    display: 'flex',
    alignItems: 'center',
    padding: '10px 16px',
    color: '#333',
    textDecoration: 'none',
    fontSize: '14px',
    transition: 'background 0.2s'
  },
  userSection: {
    display: 'flex',
    alignItems: 'center'
  },
  userName: {
    fontSize: '14px',
    color: '#ecf0f1',
    display: 'flex',
    alignItems: 'center'
  },
  roleBadge: {
    color: 'white',
    padding: '3px 10px',
    borderRadius: '4px',
    fontSize: '11px',
    fontWeight: '600',
    textTransform: 'uppercase'
  }
};

export default Navbar;
