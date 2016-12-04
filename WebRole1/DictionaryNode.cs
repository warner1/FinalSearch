using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRole1
{
    public class DictionaryNode
    {
        public char letter;
        public Dictionary<char, DictionaryNode> dict;
        public Boolean word;


        public DictionaryNode(char letter)
        {
            this.letter = letter;
            this.dict = new Dictionary<char, DictionaryNode>();
            word = false;
        }
    }
}