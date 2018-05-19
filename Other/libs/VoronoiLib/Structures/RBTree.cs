// credit https://github.com/mdally/Voronoi/blob/master/src/RBTree.h MIT LICENSE

namespace VoronoiLib.Structures
{
    public class RBTreeNode<T>
    {
        public T Data { get; internal set; }
        public RBTreeNode<T> Left { get; internal set; }
        public RBTreeNode<T> Right { get; internal set; }
        public RBTreeNode<T> Parent { get; internal set; }

        //cached ordered traversal
        public RBTreeNode<T> Previous { get; internal set; }
        public RBTreeNode<T> Next { get; internal set; }

        internal bool Red { get; set; }

        internal RBTreeNode ()
        {
            
        }
    }

    public class RBTree<T>
    {
        public RBTreeNode<T> Root { get; private set; }

        public RBTreeNode<T> InsertSuccessor(RBTreeNode<T> node, T successorData)
        {
            var successor = new RBTreeNode<T> {Data = successorData};

            RBTreeNode<T> parent;

            if (node != null)
            {
                //insert new node between node and its successor
                successor.Previous = node;
                successor.Next = node.Next;
                if (node.Next != null)
                    node.Next.Previous = successor;
                node.Next = successor;

                //insert successor into the tree
                if (node.Right != null)
                {
                    node = GetFirst(node.Right);
                    node.Left = successor;
                }
                else
                {
                    node.Right = successor;
                }
                parent = node;
            }
            else if (Root != null)
            {
                //if the node is null, successor must be inserted
                //into the left most part of the tree
                node = GetFirst(Root);
                //successor.Previous = null;
                successor.Next = node;
                node.Previous = successor;
                node.Left = successor;
                parent = node;
            }
            else
            {
                //first insert
                //successor.Previous = successor.Next = null;
                Root = successor;
                parent = null;
            }

            //successor.Left = successor.Right = null;
            successor.Parent = parent;
            successor.Red = true;

            //the magic of the red black tree
            RBTreeNode<T> grandma;
            RBTreeNode<T> aunt;
            node = successor;
            while (parent != null  && parent.Red)
            {
                grandma = parent.Parent;
                if (parent == grandma.Left)
                {
                    aunt = grandma.Right;
                    if (aunt != null && aunt.Red)
                    {
                        parent.Red = false;
                        aunt.Red = false;
                        grandma.Red = true;
                        node = grandma;
                    }
                    else
                    {
                        if (node == parent.Right)
                        {
                            RotateLeft(parent);
                            node = parent;
                            parent = node.Parent;
                        }
                        parent.Red = false;
                        grandma.Red = true;
                        RotateRight(grandma);
                    }
                }
                else
                {
                    aunt = grandma.Left;
                    if (aunt != null && aunt.Red)
                    {
                        parent.Red = false;
                        aunt.Red = false;
                        grandma.Red = true;
                        node = grandma;
                    }
                    else
                    {
                        if (node == parent.Left)
                        {
                            RotateRight(parent);
                            node = parent;
                            parent = node.Parent;
                        }
                        parent.Red = false;
                        grandma.Red = true;
                        RotateLeft(grandma);
                    }
                }
                parent = node.Parent;
            }
            Root.Red = false;
            return successor;
        }

        //TODO: Clean this up
        public void RemoveNode(RBTreeNode<T> node)
        {
            //fix up linked list structure
            if (node.Next != null)
                node.Next.Previous = node.Previous;
            if (node.Previous != null)
                node.Previous.Next = node.Next;

            //replace the node
            var original = node;
            var parent = node.Parent;
            var left = node.Left;
            var right = node.Right;

            RBTreeNode<T> next;
            //figure out what to replace this node with
            if (left == null)
                next = right;
            else if (right == null)
                next = left;
            else
                next = GetFirst(right);

            //fix up the parent relation
            if (parent != null)
            {
                if (parent.Left == node)
                    parent.Left = next;
                else
                    parent.Right = next;
            }
            else
            {
                Root = next;
            }

            bool red;
            if (left != null && right != null)
            {
                red = next.Red;
                next.Red = node.Red;
                next.Left = left;
                left.Parent = next;

                // if we reached down the tree
                if (next != right)
                {
                    parent = next.Parent;
                    next.Parent = node.Parent;

                    node = next.Right;
                    parent.Left = node;

                    next.Right = right;
                    right.Parent = next;
                }
                else
                {
                    // the direct right will replace the node
                    next.Parent = parent;
                    parent = next;
                    node = next.Right;
                }
            }
            else
            {
                red = node.Red;
                node = next;
            }

            if (node != null)
            {
                node.Parent = parent;
            }

            if (red)
            {
                return;
            }

            if (node != null && node.Red)
            {
                node.Red = false;
                return;
            }

            //node is null or black

            // fair warning this code gets nasty

            //how do we guarantee sibling is not null
            RBTreeNode<T> sibling = null;
            do
            {
                if (node == Root)
                    break;
                if (node == parent.Left)
                {
                    sibling = parent.Right;
                    if (sibling.Red)
                    {
                        sibling.Red = false;
                        parent.Red = true;
                        RotateLeft(parent);
                        sibling = parent.Right;
                    }
                    if ((sibling.Left != null && sibling.Left.Red) || (sibling.Right != null && sibling.Right.Red))
                    {
                        //pretty sure this can be sibling.Left!= null && sibling.Left.Red
                        if (sibling.Right == null || !sibling.Right.Red)
                        {
                            sibling.Left.Red = false;
                            sibling.Red = true;
                            RotateRight(sibling);
                            sibling = parent.Right;
                        }
                        sibling.Red = parent.Red;
                        parent.Red = sibling.Right.Red = false;
                        RotateLeft(parent);
                        node = Root;
                        break;
                    }
                }
                else
                {
                    sibling = parent.Left;
                    if (sibling.Red)
                    {
                        sibling.Red = false;
                        parent.Red = true;
                        RotateRight(parent);
                        sibling = parent.Left;
                    }
                    if ((sibling.Left != null && sibling.Left.Red) || (sibling.Right != null && sibling.Right.Red))
                    {
                        if (sibling.Left == null || !sibling.Left.Red)
                        {
                            sibling.Right.Red = false;
                            sibling.Red = true;
                            RotateLeft(sibling);
                            sibling = parent.Left;
                        }
                        sibling.Red = parent.Red;
                        parent.Red = sibling.Left.Red = false;
                        RotateRight(parent);
                        node = Root;
                        break;
                    }
                }
                sibling.Red = true;
                node = parent;
                parent = parent.Parent;
            } while (!node.Red);

            if (node != null)
                node.Red = false;

        }

        public static RBTreeNode<T> GetFirst(RBTreeNode<T> node)
        {
            if (node == null)
                return null;
            while (node.Left != null)
                node = node.Left;
            return node;
        }

        public static RBTreeNode<T> GetLast(RBTreeNode<T> node)
        {
            if (node == null)
                return null;
            while (node.Right != null)
                node = node.Right;
            return node;
        }

        private void RotateLeft(RBTreeNode<T> node)
        {
            var p = node;
            var q = node.Right;
            var parent = p.Parent;

            if (parent != null)
            {
                if (parent.Left == p)
                    parent.Left = q;
                else
                    parent.Right = q;
            }
            else
                Root = q;
            q.Parent = parent;
            p.Parent = q;
            p.Right = q.Left;
            if (p.Right != null)
                p.Right.Parent = p;
            q.Left = p;
        }

        private void RotateRight(RBTreeNode<T> node)
        {
            var p = node;
            var q = node.Left;
            var parent = p.Parent;
            if (parent != null)
            {
                if (parent.Left == p)
                    parent.Left = q;
                else
                    parent.Right = q;
            }
            else
                Root = q;
            q.Parent = parent;
            p.Parent = q;
            p.Left = q.Right;
            if (p.Left != null)
                p.Left.Parent = p;
            q.Right = p;
        }

    }
}
