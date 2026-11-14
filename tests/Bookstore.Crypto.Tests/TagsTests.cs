using System.Security.Cryptography;
using Bookstore.Crypto;

namespace Bookstore.Crypto.Tests;

public class TagsTests
{
    [Fact]
    public void Equal_tags_match()
    {
        byte[] tag = RandomNumberGenerator.GetBytes(32);
        Assert.True(Tags.AreEqual(tag, tag.ToArray()));
    }

    [Fact]
    public void One_flipped_bit_or_a_shorter_tag_does_not_match()
    {
        byte[] tag = RandomNumberGenerator.GetBytes(32);
        byte[] flipped = tag.ToArray();
        flipped[31] ^= 0x01;

        Assert.False(Tags.AreEqual(tag, flipped));
        Assert.False(Tags.AreEqual(tag, tag.AsSpan(0, 16)));
    }
}
