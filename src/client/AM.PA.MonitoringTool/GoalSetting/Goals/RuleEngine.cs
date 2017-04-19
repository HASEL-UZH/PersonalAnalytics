// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-27
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using System;
using System.Linq.Expressions;

namespace GoalSetting.Rules
{
    public class RuleEngine
    {
        public static Func<T, bool> CompileRule<T>(Rule r)
        {
            var param = Expression.Parameter(typeof(T));
            Expression expr = BuildExpr<T>(r, param);
            // build a lambda function Activity->bool and compile it
            return Expression.Lambda<Func<T, bool>>(expr, param).Compile();
        }

        internal static Expression BuildExpr<T>(Rule r, ParameterExpression param)
        {
            var left = MemberExpression.Property(param, r.GoalString);
            var tProp = typeof(T).GetProperty(r.GoalString).PropertyType;
            ExpressionType tBinary;
            // is the operator a known .NET operator?
            if (ExpressionType.TryParse(r.OperatorString, out tBinary))
            {
                var right = Expression.Constant(Convert.ChangeType(r.TargetValue, tProp));
                return Expression.MakeBinary(tBinary, left, right);
            }
            // use a method call, e.g. 'Contains'
            else
            {
                var method = tProp.GetMethod(r.OperatorString);
                var tParam = method.GetParameters()[0].ParameterType;
                var right = Expression.Constant(Convert.ChangeType(r.TargetValue, tParam));
                return Expression.Call(left, method, right);
            }
        }
    }

}