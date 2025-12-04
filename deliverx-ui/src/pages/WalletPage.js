import React, { useState, useEffect } from 'react';
import { getWallet, getTransactions, rechargeWallet, getTransactionCategories } from '../services/walletService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

const WalletPage = () => {
  const [wallet, setWallet] = useState(null);
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showRecharge, setShowRecharge] = useState(false);
  const [rechargeAmount, setRechargeAmount] = useState('');
  const [rechargeLoading, setRechargeLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [categoryFilter, setCategoryFilter] = useState('');

  const categories = getTransactionCategories();

  useEffect(() => {
    loadData();
  }, [page, categoryFilter]);

  const loadData = async () => {
    try {
      setLoading(true);
      const [walletData, txData] = await Promise.all([
        getWallet(),
        getTransactions(page, 15, categoryFilter || null)
      ]);
      setWallet(walletData);
      setTransactions(txData.items || []);
      setTotalPages(txData.totalPages || 1);
    } catch (err) {
      setError(err.message || 'Failed to load wallet data');
    } finally {
      setLoading(false);
    }
  };

  const handleRecharge = async () => {
    if (!rechargeAmount || parseFloat(rechargeAmount) <= 0) {
      setError('Please enter a valid amount');
      return;
    }

    try {
      setRechargeLoading(true);
      await rechargeWallet(parseFloat(rechargeAmount));
      setShowRecharge(false);
      setRechargeAmount('');
      loadData();
    } catch (err) {
      setError(err.message || 'Recharge failed');
    } finally {
      setRechargeLoading(false);
    }
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR'
    }).format(amount || 0);
  };

  const getTransactionStyle = (type) => {
    return type === 'CREDIT'
      ? { color: '#28a745', prefix: '+' }
      : { color: '#dc3545', prefix: '-' };
  };

  if (loading && !wallet) return <LoadingSpinner />;

  return (
    <div style={{ padding: '20px', maxWidth: '1000px', margin: '0 auto' }}>
      <h1>My Wallet</h1>

      {error && <ErrorMessage message={error} onClose={() => setError('')} />}

      {/* Wallet Balance Card */}
      <div style={{
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        color: 'white',
        padding: '30px',
        borderRadius: '15px',
        marginBottom: '30px',
        boxShadow: '0 10px 30px rgba(102, 126, 234, 0.3)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <p style={{ margin: '0 0 5px', opacity: 0.8 }}>Available Balance</p>
            <h2 style={{ margin: 0, fontSize: '2.5em' }}>
              {formatCurrency(wallet?.balance)}
            </h2>
            {wallet?.holdBalance > 0 && (
              <p style={{ margin: '10px 0 0', opacity: 0.8, fontSize: '0.9em' }}>
                On Hold: {formatCurrency(wallet.holdBalance)}
              </p>
            )}
          </div>
          <button
            onClick={() => setShowRecharge(true)}
            style={{
              padding: '12px 24px',
              backgroundColor: 'white',
              color: '#667eea',
              border: 'none',
              borderRadius: '8px',
              cursor: 'pointer',
              fontWeight: '600',
              fontSize: '1em'
            }}
          >
            + Add Money
          </button>
        </div>
      </div>

      {/* Quick Actions */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: '15px', marginBottom: '30px' }}>
        {[100, 500, 1000, 2000].map(amount => (
          <button
            key={amount}
            onClick={() => { setRechargeAmount(amount.toString()); setShowRecharge(true); }}
            style={{
              padding: '15px',
              background: 'white',
              border: '2px solid #667eea',
              borderRadius: '10px',
              cursor: 'pointer',
              fontWeight: '600',
              color: '#667eea'
            }}
          >
            + {formatCurrency(amount)}
          </button>
        ))}
      </div>

      {/* Transaction History */}
      <div style={{ background: 'white', borderRadius: '10px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }}>
        <div style={{ padding: '20px', borderBottom: '1px solid #e0e0e0', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h2 style={{ margin: 0 }}>Transaction History</h2>
          <select
            value={categoryFilter}
            onChange={(e) => { setCategoryFilter(e.target.value); setPage(1); }}
            style={{
              padding: '8px 15px',
              borderRadius: '5px',
              border: '1px solid #ddd'
            }}
          >
            <option value="">All Transactions</option>
            {categories.map(cat => (
              <option key={cat.value} value={cat.value}>{cat.label}</option>
            ))}
          </select>
        </div>

        {transactions.length === 0 ? (
          <p style={{ color: '#666', textAlign: 'center', padding: '40px' }}>
            No transactions yet
          </p>
        ) : (
          <>
            {transactions.map((tx) => {
              const style = getTransactionStyle(tx.transactionType);
              return (
                <div key={tx.id} style={{ padding: '15px 20px', borderBottom: '1px solid #f0f0f0', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <div>
                    <p style={{ margin: 0, fontWeight: '500' }}>{tx.description}</p>
                    <p style={{ margin: '5px 0 0', color: '#999', fontSize: '0.85em' }}>
                      {new Date(tx.createdAt).toLocaleString()} • {tx.category}
                    </p>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <p style={{ margin: 0, fontWeight: '600', color: style.color }}>
                      {style.prefix}{formatCurrency(tx.amount)}
                    </p>
                    <p style={{ margin: '5px 0 0', color: '#999', fontSize: '0.85em' }}>
                      Bal: {formatCurrency(tx.balanceAfter)}
                    </p>
                  </div>
                </div>
              );
            })}

            {/* Pagination */}
            <div style={{ display: 'flex', justifyContent: 'center', gap: '10px', padding: '20px' }}>
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                style={{
                  padding: '8px 16px',
                  border: '1px solid #ddd',
                  borderRadius: '5px',
                  background: page === 1 ? '#f5f5f5' : 'white',
                  cursor: page === 1 ? 'not-allowed' : 'pointer'
                }}
              >
                Previous
              </button>
              <span style={{ padding: '8px 16px' }}>Page {page} of {totalPages}</span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                style={{
                  padding: '8px 16px',
                  border: '1px solid #ddd',
                  borderRadius: '5px',
                  background: page === totalPages ? '#f5f5f5' : 'white',
                  cursor: page === totalPages ? 'not-allowed' : 'pointer'
                }}
              >
                Next
              </button>
            </div>
          </>
        )}
      </div>

      {/* Recharge Modal */}
      {showRecharge && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(0,0,0,0.5)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000
        }}>
          <div style={{
            background: 'white',
            padding: '30px',
            borderRadius: '15px',
            width: '90%',
            maxWidth: '400px'
          }}>
            <h2 style={{ marginTop: 0 }}>Add Money to Wallet</h2>
            <input
              type="number"
              value={rechargeAmount}
              onChange={(e) => setRechargeAmount(e.target.value)}
              placeholder="Enter amount"
              style={{
                width: '100%',
                padding: '15px',
                fontSize: '1.2em',
                borderRadius: '8px',
                border: '2px solid #ddd',
                marginBottom: '20px',
                boxSizing: 'border-box'
              }}
            />
            <div style={{ display: 'flex', gap: '10px' }}>
              <button
                onClick={() => setShowRecharge(false)}
                style={{
                  flex: 1,
                  padding: '12px',
                  border: '1px solid #ddd',
                  borderRadius: '8px',
                  background: 'white',
                  cursor: 'pointer'
                }}
              >
                Cancel
              </button>
              <button
                onClick={handleRecharge}
                disabled={rechargeLoading}
                style={{
                  flex: 1,
                  padding: '12px',
                  border: 'none',
                  borderRadius: '8px',
                  background: '#667eea',
                  color: 'white',
                  cursor: 'pointer',
                  fontWeight: '600'
                }}
              >
                {rechargeLoading ? 'Processing...' : 'Add Money'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default WalletPage;
