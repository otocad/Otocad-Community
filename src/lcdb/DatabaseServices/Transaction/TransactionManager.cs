using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace lcdb.Transaction
{
    /// <summary>
    /// 事务管理器实现
    /// </summary>
    public class TransactionManager : ITransactionManager, IDisposable
    {
        private readonly Database _database;
        private readonly List<TransactionOperation> _transactionHistory;
        private readonly object _lockObject = new object();
        private readonly ThreadLocal<IEntityTransaction> _currentTransaction;

        public IEntityTransaction CurrentTransaction
        {
            get
            {
                return _currentTransaction.Value;
            }
        }

        public TransactionManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _transactionHistory = new List<TransactionOperation>();
            _currentTransaction = new ThreadLocal<IEntityTransaction>();
        }

        public IEntityTransaction BeginTransaction(string description = null)
        {
            lock (_lockObject)
            {
                var transaction = new EntityTransaction(_database, CurrentTransaction);
                
                // 设置事务事件处理
                transaction.AfterCommit += OnTransactionCommitted;
                transaction.AfterRollback += OnTransactionRolledBack;
                
                _currentTransaction.Value = transaction;
                
                return transaction;
            }
        }

        public IEntityTransaction BeginNestedTransaction(string description = null)
        {
            lock (_lockObject)
            {
                if (CurrentTransaction == null)
                {
                    throw new InvalidOperationException("No active transaction to create nested transaction");
                }
                
                var nestedTransaction = new EntityTransaction(_database, CurrentTransaction);
                
                // 设置事务事件处理
                nestedTransaction.AfterCommit += OnNestedTransactionCommitted;
                nestedTransaction.AfterRollback += OnNestedTransactionRolledBack;
                
                _currentTransaction.Value = nestedTransaction;
                
                return nestedTransaction;
            }
        }

        public IReadOnlyList<TransactionOperation> GetTransactionHistory(int limit = 100)
        {
            lock (_lockObject)
            {
                return _transactionHistory
                    .OrderByDescending(op => op.Timestamp)
                    .Take(limit)
                    .ToList()
                    .AsReadOnly();
            }
        }

        private void OnTransactionCommitted(object sender, TransactionEventArgs e)
        {
            lock (_lockObject)
            {
                var transaction = e.Transaction;
                
                // 将事务操作添加到历史记录
                _transactionHistory.AddRange(transaction.GetOperationHistory());
                
                // 清除当前事务
                _currentTransaction.Value = null;
                
                // 清理事件处理
                transaction.AfterCommit -= OnTransactionCommitted;
                transaction.AfterRollback -= OnTransactionRolledBack;
            }
        }

        private void OnTransactionRolledBack(object sender, TransactionEventArgs e)
        {
            lock (_lockObject)
            {
                var transaction = e.Transaction;
                
                // 回滚不记录到历史，但记录回滚事件
                var rollbackOperation = new TransactionOperation
                {
                    Type = TransactionOperationType.Remove, // 使用Remove表示回滚
                    Timestamp = DateTime.Now,
                    Description = $"Transaction {transaction.TransactionId} rolled back"
                };
                
                _transactionHistory.Add(rollbackOperation);
                
                // 清除当前事务
                _currentTransaction.Value = null;
                
                // 清理事件处理
                transaction.AfterCommit -= OnTransactionCommitted;
                transaction.AfterRollback -= OnTransactionRolledBack;
            }
        }

        private void OnNestedTransactionCommitted(object sender, TransactionEventArgs e)
        {
            lock (_lockObject)
            {
                var transaction = e.Transaction;
                
                // 嵌套事务提交时，恢复到父事务
                _currentTransaction.Value = transaction.Parent;
                
                // 清理事件处理
                transaction.AfterCommit -= OnNestedTransactionCommitted;
                transaction.AfterRollback -= OnNestedTransactionRolledBack;
            }
        }

        private void OnNestedTransactionRolledBack(object sender, TransactionEventArgs e)
        {
            lock (_lockObject)
            {
                var transaction = e.Transaction;
                
                // 嵌套事务回滚时，恢复到父事务
                _currentTransaction.Value = transaction.Parent;
                
                // 记录嵌套事务回滚
                var rollbackOperation = new TransactionOperation
                {
                    Type = TransactionOperationType.Remove,
                    Timestamp = DateTime.Now,
                    Description = $"Nested transaction {transaction.TransactionId} rolled back"
                };
                
                _transactionHistory.Add(rollbackOperation);
                
                // 清理事件处理
                transaction.AfterCommit -= OnNestedTransactionCommitted;
                transaction.AfterRollback -= OnNestedTransactionRolledBack;
            }
        }

        /// <summary>
        /// 清理历史记录
        /// </summary>
        public void ClearHistory()
        {
            lock (_lockObject)
            {
                _transactionHistory.Clear();
            }
        }

        /// <summary>
        /// 获取历史记录统计信息
        /// </summary>
        public TransactionStats GetTransactionStats()
        {
            lock (_lockObject)
            {
                var stats = new TransactionStats();
                
                foreach (var operation in _transactionHistory)
                {
                    switch (operation.Type)
                    {
                        case TransactionOperationType.Add:
                            stats.TotalAddOperations++;
                            break;
                        case TransactionOperationType.Remove:
                            stats.TotalRemoveOperations++;
                            break;
                        case TransactionOperationType.Modify:
                            stats.TotalModifyOperations++;
                            break;
                        case TransactionOperationType.Move:
                            stats.TotalMoveOperations++;
                            break;
                        case TransactionOperationType.Copy:
                            stats.TotalCopyOperations++;
                            break;
                    }
                }
                
                stats.TotalOperations = _transactionHistory.Count;
                return stats;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            lock (_lockObject)
            {
                // 如果有活动事务，回滚它
                if (CurrentTransaction != null && CurrentTransaction.State == TransactionState.Active)
                {
                    CurrentTransaction.Rollback();
                }
                
                _transactionHistory.Clear();
                _currentTransaction?.Dispose();
            }
        }
    }

    /// <summary>
    /// 事务统计信息
    /// </summary>
    public class TransactionStats
    {
        public int TotalOperations { get; set; }
        public int TotalAddOperations { get; set; }
        public int TotalRemoveOperations { get; set; }
        public int TotalModifyOperations { get; set; }
        public int TotalMoveOperations { get; set; }
        public int TotalCopyOperations { get; set; }
    }
}