using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRole1
{
    public class DictionaryTree
    {
        public DictionaryNode root;

        public DictionaryTree()
        {
            root = new DictionaryNode(' ');
        }

        public void add(string s)
        {
            char[] letters = s.ToCharArray();
            DictionaryNode current = root;
            for (int i = 0; i < s.Length; i++)
            {
                if (!current.dict.ContainsKey(letters[i]))
                {
                    current.dict.Add(letters[i], new DictionaryNode(letters[i]));
                }
                current = current.dict[letters[i]];
            }
            current.word = true;
        }

        public Boolean find(string word)
        {
            Char[] letters = word.ToCharArray();
            DictionaryNode current = root;
            for (int i = 0; i < word.Length; i++)
            {
                if (current.dict.ContainsKey(letters[i]))
                {
                    current = current.dict[letters[i]];

                }
                else
                {
                    return false;
                }
            }
            if (!current.word)
            {
                return false;
            }
            return true;
        }

        public List<string> suggestions(string input)
        {
            char[] letters = input.ToCharArray();
            List<string> list = new List<string>();
            DictionaryNode current = root;
            for (int i = 0; i < input.Length; i++)
            {
                if (current.dict.ContainsKey(letters[i]))
                {
                    current = current.dict[letters[i]];
                }
                else
                {
                    return list;
                }
            }
            return search(list, current, input.Substring(0, input.Length - 1));
        }

        private List<string> search(List<string> list, DictionaryNode current, string input)
        {
            if (list.Count < 10)
            {
                input = input + current.letter;
                if (current.word)
                {
                    list.Add(input);

                }
                foreach (char c in current.dict.Keys)
                {
                    search(list, current.dict[c], input);
                }
            }
            return list;
        }
    }
}