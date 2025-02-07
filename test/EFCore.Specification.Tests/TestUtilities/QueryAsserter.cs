// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.TestUtilities.QueryTestGeneration;
using Xunit;

namespace Microsoft.EntityFrameworkCore.TestUtilities
{
    public class QueryAsserter<TContext> : QueryAsserterBase
        where TContext : DbContext
    {
        private readonly Func<TContext> _contextCreator;
        private readonly Dictionary<Type, Func<dynamic, object>> _entitySorters;
        private readonly Dictionary<Type, Action<dynamic, dynamic>> _entityAsserters;
        private readonly IncludeQueryResultAsserter _includeResultAsserter;

        private const bool ProceduralQueryGeneration = false;

        public QueryAsserter(
            Func<TContext> contextCreator,
            IExpectedData expectedData,
            Dictionary<Type, Func<dynamic, object>> entitySorters,
            Dictionary<Type, Action<dynamic, dynamic>> entityAsserters)
        {
            _contextCreator = contextCreator;
            ExpectedData = expectedData;

            _entitySorters = entitySorters ?? new Dictionary<Type, Func<dynamic, object>>();
            _entityAsserters = entityAsserters ?? new Dictionary<Type, Action<dynamic, dynamic>>();

            SetExtractor = new DefaultSetExtractor();
            _includeResultAsserter = new IncludeQueryResultAsserter(_entitySorters, _entityAsserters);
        }

        public override void AssertEqual<T>(T expected, T actual, Action<dynamic, dynamic> asserter = null)
        {
            if (asserter == null && expected != null)
            {
                _entityAsserters.TryGetValue(expected.GetType(), out asserter);
            }

            asserter ??= Assert.Equal;
            asserter(expected, actual);
        }

        public override void AssertCollection<TElement>(
            IEnumerable<TElement> expected,
            IEnumerable<TElement> actual,
            bool ordered = false)
        {
            if (expected == null !=  (actual == null))
            {
                throw new InvalidOperationException(
                    $"Nullability doesn't match. Expected: {(expected == null ? "NULL" : "NOT NULL")}. Actual: {(actual == null ? "NULL." : "NOT NULL.")}.");
            }

            _entitySorters.TryGetValue(typeof(TElement), out var elementSorter);
            _entityAsserters.TryGetValue(typeof(TElement), out var elementAsserter);
            elementAsserter ??= Assert.Equal;

            if (!ordered)
            {
                if (elementSorter != null)
                {
                    var sortedActual = ((IEnumerable<dynamic>)actual).OrderBy(elementSorter).ToList();
                    var sortedExpected = ((IEnumerable<dynamic>)expected).OrderBy(elementSorter).ToList();

                    Assert.Equal(sortedExpected.Count, sortedActual.Count);
                    for (var i = 0; i < sortedExpected.Count; i++)
                    {
                        elementAsserter(sortedExpected[i], sortedActual[i]);
                    }
                }
                else
                {
                    var sortedActual = actual.OrderBy(e => e).ToList();
                    var sortedExpected = expected.OrderBy(e => e).ToList();

                    Assert.Equal(sortedExpected.Count, sortedActual.Count);
                    for (var i = 0; i < sortedExpected.Count; i++)
                    {
                        elementAsserter(sortedExpected[i], sortedActual[i]);
                    }
                }
            }
            else
            {
                var expectedList = expected.ToList();
                var actualList = actual.ToList();

                Assert.Equal(expectedList.Count, actualList.Count);
                for (var i = 0; i < expectedList.Count; i++)
                {
                    elementAsserter(expectedList[i], actualList[i]);
                }
            }
        }

        #region AssertSingleResult

        public override async Task AssertSingleResult<TItem1>(
            Func<IQueryable<TItem1>, object> actualSyncQuery,
            Func<IQueryable<TItem1>, Task<object>> actualAsyncQuery,
            Func<IQueryable<TItem1>, object> expectedQuery,
            Action<object, object> asserter,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                object actual;

                if (isAsync)
                {
                    actual = await actualAsyncQuery(SetExtractor.Set<TItem1>(context));
                }
                else
                {
                    actual = actualSyncQuery(SetExtractor.Set<TItem1>(context));
                }

                var expected = expectedQuery(ExpectedData.Set<TItem1>());

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertSingleResult<TItem1, TResult>(
            Func<IQueryable<TItem1>, TResult> actualSyncQuery,
            Func<IQueryable<TItem1>, Task<TResult>> actualAsyncQuery,
            Func<IQueryable<TItem1>, TResult> expectedQuery,
            Action<object, object> asserter,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                object actual;

                if (isAsync)
                {
                    actual = await actualAsyncQuery(SetExtractor.Set<TItem1>(context));
                }
                else
                {
                    actual = actualSyncQuery(SetExtractor.Set<TItem1>(context));
                }

                var expected = expectedQuery(ExpectedData.Set<TItem1>());

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertSingleResult<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, object> actualSyncQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, Task<object>> actualAsyncQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, object> expectedQuery,
            Action<object, object> asserter,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                object actual;

                if (isAsync)
                {
                    actual = await actualAsyncQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context));
                }
                else
                {
                    actual = actualSyncQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context));
                }

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>());

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertSingleResult<TItem1, TItem2, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, TResult> actualSyncQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, Task<TResult>> actualAsyncQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, TResult> expectedQuery,
            Action<object, object> asserter,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                object actual;

                if (isAsync)
                {
                    actual = await actualAsyncQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context));
                }
                else
                {
                    actual = actualSyncQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context));
                }

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>());

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertSingleResult<TItem1, TItem2, TItem3>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, object> actualSyncQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, Task<object>> actualAsyncQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, object> expectedQuery,
            Action<object, object> asserter,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                object actual;

                if (isAsync)
                {
                    actual = await actualAsyncQuery(
                        SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context), SetExtractor.Set<TItem3>(context));
                }
                else
                {
                    actual = actualSyncQuery(
                        SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context), SetExtractor.Set<TItem3>(context));
                }

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>(), ExpectedData.Set<TItem3>());

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertSingleResult<TItem1, TItem2, TItem3, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, TResult> actualSyncQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, Task<TResult>> actualAsyncQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, TResult> expectedQuery,
            Action<object, object> asserter,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                object actual;

                if (isAsync)
                {
                    actual = await actualAsyncQuery(
                        SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context), SetExtractor.Set<TItem3>(context));
                }
                else
                {
                    actual = actualSyncQuery(
                        SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context), SetExtractor.Set<TItem3>(context));
                }

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>(), ExpectedData.Set<TItem3>());

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        #endregion

        #region AssertQuery

        private void OrderingSettingsVerifier(bool assertOrder, Type type)
            => OrderingSettingsVerifier(assertOrder, type, elementSorter: null);

        private void OrderingSettingsVerifier(bool assertOrder, Type type, Func<dynamic, object> elementSorter)
        {
            if (!assertOrder
                && type.IsGenericType
                && (type.GetGenericTypeDefinition() == typeof(IOrderedEnumerable<>)
                    || type.GetGenericTypeDefinition() == typeof(IOrderedQueryable<>)))
            {
                throw new InvalidOperationException(
                    "Query result is OrderedQueryable - you need to set AssertQuery option: 'assertOrder' to 'true'. If the resulting order is non-deterministic by design, add identity projection to the top of the query to disable this check.");
            }

            if (assertOrder && elementSorter != null)
            {
                throw new InvalidOperationException("You shouldn't apply element sorter when 'assertOrder' is set to 'true'.");
            }
        }

        public override async Task AssertQuery<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            Func<dynamic, object> elementSorter,
            Action<dynamic, dynamic> elementAsserter,
            bool assertOrder,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                var query = actualQuery(SetExtractor.Set<TItem1>(context));
                if (ProceduralQueryGeneration && !isAsync)
                {
                    new ProcedurallyGeneratedQueryExecutor().Execute(query, context, testMethodName);

                    return;
                }

                OrderingSettingsVerifier(assertOrder, query.Expression.Type, elementSorter);

                var actual = isAsync
                    ? await query.ToArrayAsync()
                    : query.ToArray();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).ToArray();

                var firstNonNullableElement = expected.FirstOrDefault(e => e != null);
                if (firstNonNullableElement != null)
                {
                    if (!assertOrder
                        && elementSorter == null)
                    {
                        _entitySorters.TryGetValue(firstNonNullableElement.GetType(), out elementSorter);
                    }

                    if (elementAsserter == null)
                    {
                        _entityAsserters.TryGetValue(firstNonNullableElement.GetType(), out elementAsserter);
                    }
                }

                TestHelpers.AssertResults(
                    expected,
                    actual,
                    elementSorter,
                    elementAsserter,
                    assertOrder);

                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertQuery<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> expectedQuery,
            Func<dynamic, object> elementSorter,
            Action<dynamic, dynamic> elementAsserter,
            bool assertOrder,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                var query = actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context));
                if (ProceduralQueryGeneration && !isAsync)
                {
                    new ProcedurallyGeneratedQueryExecutor().Execute(query, context, testMethodName);

                    return;
                }

                OrderingSettingsVerifier(assertOrder, query.Expression.Type, elementSorter);

                var actual = isAsync
                    ? await query.ToArrayAsync()
                    : query.ToArray();

                var expected = expectedQuery(
                    ExpectedData.Set<TItem1>(),
                    ExpectedData.Set<TItem2>()).ToArray();

                var firstNonNullableElement = expected.FirstOrDefault(e => e != null);
                if (firstNonNullableElement != null)
                {
                    if (!assertOrder
                        && elementSorter == null)
                    {
                        _entitySorters.TryGetValue(firstNonNullableElement.GetType(), out elementSorter);
                    }

                    if (elementAsserter == null)
                    {
                        _entityAsserters.TryGetValue(firstNonNullableElement.GetType(), out elementAsserter);
                    }
                }

                TestHelpers.AssertResults(
                    expected,
                    actual,
                    elementSorter,
                    elementAsserter,
                    assertOrder);

                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertQuery<TItem1, TItem2, TItem3>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, IQueryable<object>> expectedQuery,
            Func<dynamic, object> elementSorter,
            Action<dynamic, dynamic> elementAsserter,
            bool assertOrder,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                var query = actualQuery(
                    SetExtractor.Set<TItem1>(context),
                    SetExtractor.Set<TItem2>(context),
                    SetExtractor.Set<TItem3>(context));

                if (ProceduralQueryGeneration && !isAsync)
                {
                    new ProcedurallyGeneratedQueryExecutor().Execute(query, context, testMethodName);

                    return;
                }

                var actual = isAsync
                    ? await query.ToArrayAsync()
                    : query.ToArray();

                var expected = expectedQuery(
                    ExpectedData.Set<TItem1>(),
                    ExpectedData.Set<TItem2>(),
                    ExpectedData.Set<TItem3>()).ToArray();

                var firstNonNullableElement = expected.FirstOrDefault(e => e != null);
                if (firstNonNullableElement != null)
                {
                    if (!assertOrder
                        && elementSorter == null)
                    {
                        _entitySorters.TryGetValue(firstNonNullableElement.GetType(), out elementSorter);
                    }

                    if (elementAsserter == null)
                    {
                        _entityAsserters.TryGetValue(firstNonNullableElement.GetType(), out elementAsserter);
                    }
                }

                TestHelpers.AssertResults(
                    expected,
                    actual,
                    elementSorter,
                    elementAsserter,
                    assertOrder);

                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        #endregion

        #region AssertQueryScalar

        // one argument

        public virtual Task AssertQueryScalar<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<int>> query,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            => AssertQueryScalar(query, query, assertOrder, isAsync, testMethodName);

        public virtual Task AssertQueryScalar<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<int>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<int>> expectedQuery,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            => AssertQueryScalar<TItem1, int>(actualQuery, expectedQuery, assertOrder, isAsync, testMethodName);

        public virtual Task AssertQueryScalar<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<long>> query,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            => AssertQueryScalar(query, query, assertOrder, isAsync, testMethodName);

        public virtual Task AssertQueryScalar<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<short>> query,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            => AssertQueryScalar(query, query, assertOrder, isAsync, testMethodName);

        public virtual Task AssertQueryScalarAsync<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<bool>> query,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            => AssertQueryScalar(query, query, assertOrder, isAsync, testMethodName);

        public virtual Task AssertQueryScalarAsync<TItem1, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TResult>> query,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            where TResult : struct
            => AssertQueryScalar(query, query, assertOrder, isAsync, testMethodName);

        public override async Task AssertQueryScalar<TItem1, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TResult>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TResult>> expectedQuery,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                var query = actualQuery(SetExtractor.Set<TItem1>(context));
                if (ProceduralQueryGeneration && !isAsync)
                {
                    new ProcedurallyGeneratedQueryExecutor().Execute(query, context, testMethodName);

                    return;
                }

                OrderingSettingsVerifier(assertOrder, query.Expression.Type);

                var actual = isAsync
                    ? await query.ToArrayAsync()
                    : query.ToArray();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).ToArray();

                TestHelpers.AssertResults(
                    expected,
                    actual,
                    e => e,
                    Assert.Equal,
                    assertOrder);
            }
        }

        // two arguments

        public virtual Task AssertQueryScalar<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<int>> query,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            where TItem2 : class
            => AssertQueryScalar(query, query, assertOrder, isAsync, testMethodName);

        public virtual Task AssertQueryScalar<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<int>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<int>> expectedQuery,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            where TItem2 : class
            => AssertQueryScalar<TItem1, TItem2, int>(actualQuery, expectedQuery, assertOrder, isAsync, testMethodName);

        public override async Task AssertQueryScalar<TItem1, TItem2, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TResult>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TResult>> expectedQuery,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                var query = actualQuery(
                    SetExtractor.Set<TItem1>(context),
                    SetExtractor.Set<TItem2>(context));

                if (ProceduralQueryGeneration && !isAsync)
                {
                    new ProcedurallyGeneratedQueryExecutor().Execute(query, context, testMethodName);

                    return;
                }

                var actual = isAsync
                    ? await query.ToArrayAsync()
                    : query.ToArray();

                var expected = expectedQuery(
                    ExpectedData.Set<TItem1>(),
                    ExpectedData.Set<TItem2>()).ToArray();

                TestHelpers.AssertResults(
                    expected,
                    actual,
                    e => e,
                    Assert.Equal,
                    assertOrder);
            }
        }

        // three arguments

        public virtual Task AssertQueryScalar<TItem1, TItem2, TItem3>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, IQueryable<int>> query,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            where TItem2 : class
            where TItem3 : class
            => AssertQueryScalar(query, query, assertOrder, isAsync, testMethodName);

        public override async Task AssertQueryScalar<TItem1, TItem2, TItem3, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, IQueryable<TResult>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, IQueryable<TResult>> expectedQuery,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                var query = actualQuery(
                    SetExtractor.Set<TItem1>(context),
                    SetExtractor.Set<TItem2>(context),
                    SetExtractor.Set<TItem3>(context));

                if (ProceduralQueryGeneration && !isAsync)
                {
                    new ProcedurallyGeneratedQueryExecutor().Execute(query, context, testMethodName);

                    return;
                }

                var actual = isAsync
                    ? await query.ToArrayAsync()
                    : query.ToArray();

                var expected = expectedQuery(
                    ExpectedData.Set<TItem1>(),
                    ExpectedData.Set<TItem2>(),
                    ExpectedData.Set<TItem3>()).ToArray();

                TestHelpers.AssertResults(
                    expected,
                    actual,
                    e => e,
                    Assert.Equal,
                    assertOrder);
            }
        }

        #endregion

        #region AssertQueryNullableScalar

        // one argument

        public virtual Task AssertQueryScalar<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<int?>> query,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            => AssertQueryScalar(query, query, assertOrder, isAsync, testMethodName);

        public virtual Task AssertQueryScalar<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<int?>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<int?>> expectedQuery,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            => AssertQueryScalar<TItem1, int>(actualQuery, expectedQuery, assertOrder, isAsync, testMethodName);

        // NB: Using Nullable<> instead of ? to work around dotnet/roslyn#31676
        public override async Task AssertQueryScalar<TItem1, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TResult?>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TResult?>> expectedQuery,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                var query = actualQuery(SetExtractor.Set<TItem1>(context));
                if (ProceduralQueryGeneration && !isAsync)
                {
                    new ProcedurallyGeneratedQueryExecutor().Execute(query, context, testMethodName);

                    return;
                }

                OrderingSettingsVerifier(assertOrder, query.Expression.Type);

                var actual = isAsync
                    ? await query.ToArrayAsync()
                    : query.ToArray();
                var expected = expectedQuery(ExpectedData.Set<TItem1>()).ToArray();

                TestHelpers.AssertResultsNullable(
                    expected,
                    actual,
                    e => e,
                    Assert.Equal,
                    assertOrder);
            }
        }

        // two arguments

        public virtual Task AssertQueryScalar<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<int?>> query,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            where TItem2 : class
            => AssertQueryScalar(query, query, assertOrder, isAsync, testMethodName);

        public virtual Task AssertQueryScalar<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<int?>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<int?>> expectedQuery,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            where TItem2 : class
            => AssertQueryScalar<TItem1, TItem2, int>(actualQuery, expectedQuery, assertOrder, isAsync, testMethodName);

        // NB: Using Nullable<> instead of ? to work around dotnet/roslyn#31676
        public override async Task AssertQueryScalar<TItem1, TItem2, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TResult?>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TResult?>> expectedQuery,
            bool assertOrder,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                var query = actualQuery(
                    SetExtractor.Set<TItem1>(context),
                    SetExtractor.Set<TItem2>(context));

                if (ProceduralQueryGeneration && !isAsync)
                {
                    new ProcedurallyGeneratedQueryExecutor().Execute(query, context, testMethodName);

                    return;
                }

                OrderingSettingsVerifier(assertOrder, query.Expression.Type);

                var actual = isAsync
                    ? await query.ToArrayAsync()
                    : query.ToArray();

                var expected = expectedQuery(
                    ExpectedData.Set<TItem1>(),
                    ExpectedData.Set<TItem2>()).ToArray();

                TestHelpers.AssertResultsNullable(
                    expected,
                    actual,
                    e => e,
                    Assert.Equal,
                    assertOrder);
            }
        }

        #endregion

        #region AssertIncludeQuery

        public Task<List<object>> AssertIncludeQuery<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> query,
            List<IExpectedInclude> expectedIncludes,
            Func<dynamic, object> elementSorter,
            List<Func<dynamic, object>> clientProjections,
            bool assertOrder,
            int entryCount,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            => AssertIncludeQuery(
                query, query, expectedIncludes, elementSorter, clientProjections, assertOrder, entryCount, isAsync, testMethodName);

        public override async Task<List<object>> AssertIncludeQuery<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            List<IExpectedInclude> expectedIncludes,
            Func<dynamic, object> elementSorter,
            List<Func<dynamic, object>> clientProjections,
            bool assertOrder,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                var query = actualQuery(SetExtractor.Set<TItem1>(context));
                if (ProceduralQueryGeneration && !isAsync)
                {
                    new ProcedurallyGeneratedQueryExecutor().Execute(query, context, testMethodName);

                    return default;
                }

                OrderingSettingsVerifier(assertOrder, query.Expression.Type, elementSorter);

                var actual = isAsync
                    ? await query.ToListAsync()
                    : query.ToList();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).ToList();

                if (!assertOrder)
                {
                    if (elementSorter == null)
                    {
                        var firstNonNullableElement = expected.FirstOrDefault(e => e != null);
                        if (firstNonNullableElement != null)
                        {
                            _entitySorters.TryGetValue(firstNonNullableElement.GetType(), out elementSorter);
                        }
                    }

                    if (elementSorter != null)
                    {
                        actual = actual.OrderBy(elementSorter).ToList();
                        expected = expected.OrderBy(elementSorter).ToList();
                    }
                }

                if (clientProjections != null)
                {
                    foreach (var clientProjection in clientProjections)
                    {
                        var projectedActual = actual.Select(clientProjection).ToList();
                        var projectedExpected = expected.Select(clientProjection).ToList();

                        _includeResultAsserter.AssertResult(projectedExpected, projectedActual, expectedIncludes);
                    }
                }
                else
                {
                    _includeResultAsserter.AssertResult(expected, actual, expectedIncludes);
                }

                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());

                return actual;
            }
        }

        public Task<List<object>> AssertIncludeQuery<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> query,
            List<IExpectedInclude> expectedIncludes,
            Func<dynamic, object> elementSorter,
            List<Func<dynamic, object>> clientProjections,
            bool assertOrder,
            int entryCount,
            bool isAsync,
            string testMethodName)
            where TItem1 : class
            where TItem2 : class
            => AssertIncludeQuery(
                query, query, expectedIncludes, elementSorter, clientProjections, assertOrder, entryCount, isAsync, testMethodName);

        public override async Task<List<object>> AssertIncludeQuery<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> expectedQuery,
            List<IExpectedInclude> expectedIncludes,
            Func<dynamic, object> elementSorter,
            List<Func<dynamic, object>> clientProjections,
            bool assertOrder,
            int entryCount,
            bool isAsync,
            string testMethodName)
        {
            using (var context = _contextCreator())
            {
                var query = actualQuery(
                    SetExtractor.Set<TItem1>(context),
                    SetExtractor.Set<TItem2>(context));

                if (ProceduralQueryGeneration && !isAsync)
                {
                    new ProcedurallyGeneratedQueryExecutor().Execute(query, context, testMethodName);

                    return default;
                }

                var actual = isAsync
                    ? await query.ToListAsync()
                    : query.ToList();

                var expected = expectedQuery(
                    ExpectedData.Set<TItem1>(),
                    ExpectedData.Set<TItem2>()).ToList();

                if (!assertOrder)
                {
                    if (elementSorter == null)
                    {
                        var firstNonNullableElement = expected.FirstOrDefault(e => e != null);
                        if (firstNonNullableElement != null)
                        {
                            _entitySorters.TryGetValue(firstNonNullableElement.GetType(), out elementSorter);
                        }
                    }

                    if (elementSorter != null)
                    {
                        actual = actual.OrderBy(elementSorter).ToList();
                        expected = expected.OrderBy(elementSorter).ToList();
                    }
                }

                if (clientProjections != null)
                {
                    foreach (var clientProjection in clientProjections)
                    {
                        var projectedActual = actual.Select(clientProjection).ToList();
                        var projectedExpected = expected.Select(clientProjection).ToList();

                        _includeResultAsserter.AssertResult(projectedExpected, projectedActual, expectedIncludes);
                    }
                }
                else
                {
                    _includeResultAsserter.AssertResult(expected, actual, expectedIncludes);
                }

                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());

                return actual;
            }
        }

        #endregion

        #region AssertAny

        public override async Task AssertAny<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AnyAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Any();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Any();

                Assert.Equal(expected, actual);
            }
        }

        public override async Task AssertAny<TItem1, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TResult>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TResult>> expectedQuery,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AnyAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Any();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Any();

                Assert.Equal(expected, actual);
            }
        }

        public override async Task AssertAny<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> expectedQuery,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).AnyAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).Any();

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>()).Any();

                Assert.Equal(expected, actual);
            }
        }

        public override async Task AssertAny<TItem1, TItem2, TItem3>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, IQueryable<object>> expectedQuery,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(
                        SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context), SetExtractor.Set<TItem3>(context)).AnyAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context), SetExtractor.Set<TItem3>(context))
                        .Any();

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>(), ExpectedData.Set<TItem3>()).Any();

                Assert.Equal(expected, actual);
            }
        }

        public override async Task AssertAny<TItem1, TPredicate>(
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> expectedQuery,
            Expression<Func<TPredicate, bool>> actualPredicate,
            Expression<Func<TPredicate, bool>> expectedPredicate,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AnyAsync(actualPredicate)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Any(actualPredicate);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Any(expectedPredicate);

                Assert.Equal(expected, actual);
            }
        }

        #endregion

        #region AssertAll

        public override async Task AssertAll<TItem1, TPredicate>(
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> expectedQuery,
            Expression<Func<TPredicate, bool>> actualPredicate,
            Expression<Func<TPredicate, bool>> expectedPredicate,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AllAsync(actualPredicate)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).All(actualPredicate);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).All(expectedPredicate);

                Assert.Equal(expected, actual);
            }
        }

        #endregion

        #region AssertFirst

        public override async Task AssertFirst<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).FirstAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).First();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).First();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertFirst<TItem1, TPredicate>(
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> expectedQuery,
            Expression<Func<TPredicate, bool>> actualFirstPredicate,
            Expression<Func<TPredicate, bool>> expectedFirstPredicate,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).FirstAsync(actualFirstPredicate)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).First(actualFirstPredicate);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).First(expectedFirstPredicate);

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        #endregion

        #region AssertFirstOrDefault

        public override async Task AssertFirstOrDefault<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).FirstOrDefaultAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).FirstOrDefault();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).FirstOrDefault();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertFirstOrDefault<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).FirstOrDefaultAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).FirstOrDefault();

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>()).FirstOrDefault();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertFirstOrDefault<TItem1, TItem2, TItem3>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TItem3>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(
                            SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context), SetExtractor.Set<TItem3>(context))
                        .FirstOrDefaultAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context), SetExtractor.Set<TItem3>(context))
                        .FirstOrDefault();

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>(), ExpectedData.Set<TItem3>())
                    .FirstOrDefault();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertFirstOrDefault<TItem1, TPredicate>(
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> expectedQuery,
            Expression<Func<TPredicate, bool>> actualPredicate,
            Expression<Func<TPredicate, bool>> expectedPredicate,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).FirstOrDefaultAsync(actualPredicate)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).FirstOrDefault(actualPredicate);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).FirstOrDefault(expectedPredicate);

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        #endregion

        #region AssertSingle

        public override async Task AssertSingle<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SingleAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Single();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Single();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertSingle<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).SingleAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).Single();

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>()).Single();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertSingle<TItem1, TPredicate>(
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> expectedQuery,
            Expression<Func<TPredicate, bool>> actualFirstPredicate,
            Expression<Func<TPredicate, bool>> expectedFirstPredicate,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SingleAsync(actualFirstPredicate)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Single(actualFirstPredicate);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Single(expectedFirstPredicate);

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        #endregion

        #region AssertSingleOrDefault

        public override async Task AssertSingleOrDefault<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SingleOrDefaultAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).SingleOrDefault();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).SingleOrDefault();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertSingleOrDefault<TItem1, TPredicate>(
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> expectedQuery,
            Expression<Func<TPredicate, bool>> actualPredicate,
            Expression<Func<TPredicate, bool>> expectedPredicate,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SingleOrDefaultAsync(actualPredicate)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).SingleOrDefault(actualPredicate);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).SingleOrDefault(expectedPredicate);

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        #endregion

        #region AssertLast

        public override async Task AssertLast<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).LastAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Last();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Last();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertLast<TItem1, TPredicate>(
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> expectedQuery,
            Expression<Func<TPredicate, bool>> actualPredicate,
            Expression<Func<TPredicate, bool>> expectedPredicate,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).LastAsync(actualPredicate)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Last(actualPredicate);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Last(expectedPredicate);

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        #endregion

        #region AssertLastOrDefault

        public override async Task AssertLastOrDefault<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).LastOrDefaultAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).LastOrDefault();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).LastOrDefault();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertLastOrDefault<TItem1, TPredicate>(
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> expectedQuery,
            Expression<Func<TPredicate, bool>> actualPredicate,
            Expression<Func<TPredicate, bool>> expectedPredicate,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).LastOrDefaultAsync(actualPredicate)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).LastOrDefault(actualPredicate);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).LastOrDefault(expectedPredicate);

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        #endregion

        #region AssertCount

        public override async Task AssertCount<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).CountAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Count();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Count();

                Assert.Equal(expected, actual);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertCount<TItem1, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TResult>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TResult>> expectedQuery,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).CountAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Count();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Count();

                Assert.Equal(expected, actual);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertCount<TItem1, TPredicate>(
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> expectedQuery,
            Expression<Func<TPredicate, bool>> actualPredicate,
            Expression<Func<TPredicate, bool>> expectedPredicate,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).CountAsync(actualPredicate)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Count(actualPredicate);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Count(expectedPredicate);

                Assert.Equal(expected, actual);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertCount<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> expectedQuery,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).CountAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).Count();

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>()).Count();

                Assert.Equal(expected, actual);
            }
        }

        #endregion

        #region AssertLongCount

        public override async Task AssertLongCount<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).LongCountAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).LongCount();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).LongCount();

                Assert.Equal(expected, actual);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertLongCount<TItem1, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TResult>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TResult>> expectedQuery,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).LongCountAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).LongCount();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).LongCount();

                Assert.Equal(expected, actual);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertLongCount<TItem1, TPredicate>(
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TPredicate>> expectedQuery,
            Expression<Func<TPredicate, bool>> actualPredicate,
            Expression<Func<TPredicate, bool>> expectedPredicate,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).LongCountAsync(actualPredicate)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).LongCount(actualPredicate);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).LongCount(expectedPredicate);

                Assert.Equal(expected, actual);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertLongCount<TItem1, TItem2>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<object>> expectedQuery,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).LongCountAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).LongCount();

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>()).LongCount();

                Assert.Equal(expected, actual);
            }
        }

        #endregion

        #region AssertMin

        public override async Task AssertMin<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).MinAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Min();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Min();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertMin<TItem1, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TResult>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TResult>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).MinAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Min();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Min();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertMin<TItem1, TSelector, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, TResult>> actualSelector,
            Expression<Func<TSelector, TResult>> expectedSelector,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).MinAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Min(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Min(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        #endregion

        #region AssertMax

        public override async Task AssertMax<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<object>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<object>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).MaxAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Max();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Max();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertMax<TItem1, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TResult>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TResult>> expectedQuery,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).MaxAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Max();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Max();

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        public override async Task AssertMax<TItem1, TSelector, TResult>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, TResult>> actualSelector,
            Expression<Func<TSelector, TResult>> expectedSelector,
            Action<object, object> asserter = null,
            int entryCount = 0,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).MaxAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Max(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Max(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Equal(entryCount, context.ChangeTracker.Entries().Count());
            }
        }

        #endregion

        #region AssertSum

        public override async Task AssertSum<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<int>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<int>> expectedQuery,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SumAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Sum();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Sum();

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertSum<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<int?>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<int?>> expectedQuery,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SumAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Sum();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Sum();

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertSum<TItem1, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, int>> actualSelector,
            Expression<Func<TSelector, int>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SumAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Sum(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Sum(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertSum<TItem1, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, int?>> actualSelector,
            Expression<Func<TSelector, int?>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SumAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Sum(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Sum(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertSum<TItem1, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, long>> actualSelector,
            Expression<Func<TSelector, long>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SumAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Sum(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Sum(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertSum<TItem1, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, long?>> actualSelector,
            Expression<Func<TSelector, long?>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SumAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Sum(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Sum(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertSum<TItem1, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, decimal>> actualSelector,
            Expression<Func<TSelector, decimal>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SumAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Sum(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Sum(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertSum<TItem1, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, float>> actualSelector,
            Expression<Func<TSelector, float>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).SumAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Sum(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Sum(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertSum<TItem1, TItem2, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TItem2>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, int>> actualSelector,
            Expression<Func<TSelector, int>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).SumAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context), SetExtractor.Set<TItem2>(context)).Sum(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>(), ExpectedData.Set<TItem2>()).Sum(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        #endregion

        #region AssertAverage

        public override async Task AssertAverage<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<int>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<int>> expectedQuery,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AverageAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Average();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Average();

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertAverage<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<int?>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<int?>> expectedQuery,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AverageAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Average();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Average();

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertAverage<TItem1>(
            Func<IQueryable<TItem1>, IQueryable<long>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<long>> expectedQuery,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AverageAsync()
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Average();

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Average();

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertAverage<TItem1, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, int>> actualSelector,
            Expression<Func<TSelector, int>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AverageAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Average(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Average(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertAverage<TItem1, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, int?>> actualSelector,
            Expression<Func<TSelector, int?>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AverageAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Average(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Average(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertAverage<TItem1, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, decimal>> actualSelector,
            Expression<Func<TSelector, decimal>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AverageAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Average(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Average(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        public override async Task AssertAverage<TItem1, TSelector>(
            Func<IQueryable<TItem1>, IQueryable<TSelector>> actualQuery,
            Func<IQueryable<TItem1>, IQueryable<TSelector>> expectedQuery,
            Expression<Func<TSelector, float>> actualSelector,
            Expression<Func<TSelector, float>> expectedSelector,
            Action<object, object> asserter = null,
            bool isAsync = false)
        {
            using (var context = _contextCreator())
            {
                var actual = isAsync
                    ? await actualQuery(SetExtractor.Set<TItem1>(context)).AverageAsync(actualSelector)
                    : actualQuery(SetExtractor.Set<TItem1>(context)).Average(actualSelector);

                var expected = expectedQuery(ExpectedData.Set<TItem1>()).Average(expectedSelector);

                AssertEqual(expected, actual, asserter);
                Assert.Empty(context.ChangeTracker.Entries());
            }
        }

        #endregion

        private class DefaultSetExtractor : ISetExtractor
        {
            public override IQueryable<TEntity> Set<TEntity>(DbContext context)
            {
                return context.Set<TEntity>();
            }
        }
    }
}
