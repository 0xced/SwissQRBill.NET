//
// Swiss QR Bill Generator for .NET
// Copyright (c) 2018 Manuel Bleichenbacher
// Licensed under MIT License
// https://opensource.org/licenses/MIT
//

#nullable enable

using System;
using System.Collections.Generic;

namespace Codecrete.SwissQRBill.Generator
{
    /// <summary>
    /// Alternative payment scheme instructions
    /// </summary>
    public sealed class AlternativeScheme : IEquatable<AlternativeScheme>
    {
        /// <summary>
        /// Initializes a new instance with the specified payment instruction.
        /// </summary>
        /// <param name="instruction">The payment instruction.</param>
        public AlternativeScheme(string instruction) : this("", instruction)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified name and payment instruction.
        /// </summary>
        /// <param name="name">The payment scheme name.</param>
        /// <param name="instruction">The payment instruction.</param>
        public AlternativeScheme(string name, string instruction)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Instruction = instruction ?? throw new ArgumentNullException(nameof(instruction));
        }

        /// <summary>
        /// Gets or sets the payment scheme name.
        /// </summary>
        /// <value>The payment scheme name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the payment instruction for a given bill.
        /// <para>
        /// The instruction consists of a two letter abbreviation for the scheme, a separator characters
        /// and a sequence of parameters(separated by the character at index 2).
        /// </para>
        /// </summary>
        /// <value>The payment instruction.</value>
        public string Instruction { get; }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as AlternativeScheme);
        }

        /// <summary>Determines whether the specified alternative scheme is equal to the current alternative scheme.</summary>
        /// <param name="other">The alternative scheme to compare with the current alternative scheme.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public bool Equals(AlternativeScheme? other)
        {
            return other != null &&
                   Name == other.Name &&
                   Instruction == other.Instruction;
        }

        /// <summary>Gets the hash code for this instance.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            EqualityComparer<string> stringComparer = EqualityComparer<string>.Default;
            int hashCode = -1893642763;
            hashCode = hashCode * -1521134295 + stringComparer.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + stringComparer.GetHashCode(Instruction);
            return hashCode;
        }
    }
}
