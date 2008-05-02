//
// Gendarme.Rules.Design.ConsiderConvertingFieldToNullableRule
//
// Authors:
//	Cedric Vivier <cedricv@neonux.com>
//
// Copyright (c) 2008 Cedric Vivier
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

using Mono.Cecil;

using Gendarme.Framework;
using Gendarme.Framework.Rocks;

namespace Gendarme.Rules.Design {

	[Problem ("This field looks like a candidate to be a nullable.")]
	[Solution ("If possible change this field into a nullable, otherwise you can ignore the rule.")]
	public class ConsiderConvertingFieldToNullableRule : Rule, ITypeRule {

		public override void Initialize (IRunner runner)
		{
			base.Initialize (runner);

			// Nullable cannot be used if the assembly target runtime is earlier than 2.0
			Runner.AnalyzeAssembly += delegate (object o, RunnerEventArgs e) {
				Active = (e.CurrentAssembly.Runtime >= TargetRuntime.NET_2_0);
			};
		}

		static bool StartsWith (string start, string name)
		{
			return name.StartsWith (start, true, null);
		}

		protected static bool IsHasField (FieldDefinition fd, ref string prefix, ref string suffix)
		{
			if (fd.FieldType.FullName != "System.Boolean")
				return false;

			string name = fd.Name;
			if (name.Length < 4)
				return false;

			if (ExtractRemainder (name, "has", ref suffix)) {
				prefix = string.Empty;
				return true;
			}
			if (ExtractRemainder (name, "_has", ref suffix)) {
				prefix = "_";
				return true;
			}
			if (ExtractRemainder (name, "m_has", ref suffix)) {
				prefix = "m_";
				return true;
			}

			return false;
		}

		protected static bool ExtractRemainder (string full, string prefix, ref string suffix)
		{
			if (full.Length > prefix.Length && StartsWith(prefix, full)) {
				suffix = full.Substring(prefix.Length);
				return true;
			}
			return false;
		}

		public RuleResult CheckType (TypeDefinition type)
		{
			//nullables do not exist on NET<2.0 so this rule does not apply
			if (type.Module.Assembly.Runtime < TargetRuntime.NET_2_0)
				return RuleResult.DoesNotApply;
			if (type.IsEnum || type.IsGeneratedCode ())
				return RuleResult.DoesNotApply;

			//collect *has* fields
			foreach (FieldDefinition fd in type.Fields) {
				if (!fd.FieldType.IsValueType || fd.IsSpecialName || fd.HasConstant || fd.IsInitOnly)
					continue;

				string prefix = null, suffix = null;
				if (IsHasField(fd, ref prefix, ref suffix)
					&& HasValueTypeField(type, string.Concat(prefix,suffix)) ) {
					//TODO: check if they are both used in the same method? does the complexity worth it?
					string s = (Runner.VerbosityLevel > 0)
						? String.Format ("Field '{0}' should probably be a nullable if '{1}' purpose is to inform if '{0}' has been set.", fd.Name, suffix)
						: string.Empty;
					Runner.Report (fd, Severity.Low, Confidence.Low, s);
				}
			}

			return Runner.CurrentRuleResult;
		}

		private static bool HasValueTypeField (TypeDefinition type, string name)
		{
			return (null != GetValueTypeField (type, name));
		}

		public static FieldDefinition GetValueTypeField (TypeDefinition type, string name)
		{
			foreach (FieldDefinition field in type.Fields) {
				if (field.FieldType.IsValueType
					&& "System.Nullable`1" != field.FieldType.GetOriginalType().FullName
					&& 0 == string.Compare(name, field.Name, StringComparison.OrdinalIgnoreCase))
					return field;
			}
			return null;
		}

	}

}

