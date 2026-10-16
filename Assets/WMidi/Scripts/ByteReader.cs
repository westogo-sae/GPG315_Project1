using System;
using UnityEngine;

namespace Westhouse.WMidiTools
{

	public class ByteReader
	{
	 	public static string ReadAsString(byte[] bytes, int start, int length)
	    {
	    	string str = "";

	    	for(int i = start; i < start + length; i++)
	    	{
	    		str += (char)bytes[i];
	    	}
	    	return str;
	    }

	    public static string ReadAsDump(byte[] bytes, int start, int length)
	    {
	        string str = "";

	        for(int i = start; i < start + length; i++)
	        {
	            str += bytes[i].ToString("X2") + " ";
	        }
	        return str;
	    }

	    public static string ReadAsBin(byte[] bytes, int start, int length)
	    {
	        string str = "";

	        for(int i = start; i < start + length; i++)
	        {
	            str += Convert.ToString(bytes[i], 2).PadLeft(8, '0') + " ";
	        }
	        return str;
	    }

	    // Read as Big-Endian 16 bit integer
	    public static int ReadAsInt16(byte[] bytes, int start)
	    {
	        //most significant bit in first index
	        return  (bytes[start] << 8) |
	                (bytes[start + 1]);
	    }

	    // Read as Big-Endian 32 bit integer
	    public static int ReadAsInt32(byte[] bytes, int start)
	    {
	        //most significant bit in first index
	        return  (bytes[start] << 24) |
	                (bytes[start + 1] << 16) |
	                (bytes[start + 2] << 8) |
	                (bytes[start + 3]);
	    }

	    public static int ReadVarInt(byte[] bytes, int start, out int varLength)
	    {
	        varLength = 0;
	        int value = 0;

	        do
	        {
	            value = (value << 7); // shift current value accross 7 bits

	            value += bytes[start + varLength] & 127; //127 equivelent to 01111111 to remove high order bit

	        }
	        while((bytes[start + varLength++ ] & 128) == 128); // 128 equivelent to 100000000
	        // while the highest order bit is 1
	        // additionally increment varlength after comaprison
	        
	        return value;
	    }   
	}
}
