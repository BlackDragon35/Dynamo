﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using ProtoCore.AST;
using ProtoCore.AST.AssociativeAST;
using System.IO;

namespace EmitMSIL
{
    public class CodeGenIL
    {
        private ILGenerator ilGen;
        private string className;
        private string methodName;
        private IDictionary<string, IList> input;
        private IDictionary<string, IList> output;
        private int localVarIndex = -1;
        private Dictionary<string, Tuple<int, Type>> variables = new Dictionary<string, Tuple<int, Type>>();
        private StreamWriter writer;

        public CodeGenIL(IDictionary<string, IList> input, string filePath)
        {
            this.input = input;
            writer = new StreamWriter(filePath);
        }

        public void Emit(List<AssociativeNode> astList)
        {
            // 1. Create assembly builder (dynamic assembly)
            var asm = BuilderHelper.CreateAssemblyBuilder("DynamicAssembly", false);
            // 2. Create module builder
            var mod = BuilderHelper.CreateDLLModuleBuilder(asm, "DynamicModule");
            // 3. Create type builder (name it "ExecuteIL")
            var type = BuilderHelper.CreateType(mod, "ExecuteIL");
            // 4. Create method ("Execute"), get ILGenerator 
            var execMethod = BuilderHelper.CreateMethod(type, "Execute",
                System.Reflection.MethodAttributes.Static, typeof(void), new[] { typeof(IDictionary<string, IList>),
                    typeof(IDictionary<string, IList>)});
            ilGen = execMethod.GetILGenerator();

            // 5. Traverse AST and use ILGen to emit code for Execute method
            foreach(var ast in astList)
            {
                DfsTraverse(ast);
            }
            EmitOpCode(OpCodes.Ret);

            // Invoke emitted method (ExecuteIL.Execute)
            var t = type.CreateType();
            var mi = t.GetMethod("Execute");
            var output = new Dictionary<string, IList>();
            var obj = mi.Invoke(null, new[] { null, output });

            writer.Close();
            asm.Save("DynamicAssembly.dll");
        }


        public Type DfsTraverse(Node n)
        {
            Type t = null;
            if (!(n is AssociativeNode node) || node.skipMe)
                return t;

            switch (node.Kind)
            {
                case AstKind.Identifier:
                case AstKind.TypedIdentifier:
                    t = EmitIdentifierNode(node);
                    break;
                case AstKind.Integer:
                    t = EmitIntNode(node);
                    break;
                case AstKind.Double:
                    t = EmitDoubleNode(node);
                    break;
                case AstKind.Boolean:
                    t = EmitBooleanNode(node);
                    break;
                case AstKind.Char:
                    t = EmitCharNode(node);
                    break;
                case AstKind.String:
                    t = EmitStringNode(node);
                    break;
                case AstKind.DefaultArgument:
                    EmitDefaultArgNode();
                    break;
                case AstKind.Null:
                    t = EmitNullNode(node);
                    break;
                case AstKind.RangeExpression:
                    EmitRangeExprNode(node);
                    break;
                case AstKind.LanguageBlock:
                    EmitLanguageBlockNode(node);
                    break;
                case AstKind.ClassDeclaration:
                    EmitClassDeclNode(node);
                    break;
                case AstKind.Constructor:
                    EmitConstructorDefinitionNode(node);
                    break;
                case AstKind.FunctionDefintion:
                    EmitFunctionDefinitionNode(node);
                    break;
                case AstKind.FunctionCall:
                    t = EmitFunctionCallNode(node);
                    break;
                case AstKind.FunctionDotCall:
                    EmitFunctionCallNode(node);
                    break;
                case AstKind.ExpressionList:
                    EmitExprListNode(node);
                    break;
                case AstKind.IdentifierList:
                    t = EmitIdentifierListNode(node);
                    break;
                case AstKind.InlineConditional:
                    EmitInlineConditionalNode(node);
                    break;
                case AstKind.UnaryExpression:
                    EmitUnaryExpressionNode(node);
                    break;
                case AstKind.BinaryExpression:
                    EmitBinaryExpressionNode(node);
                    break;
                case AstKind.Import:
                    EmitImportNode(node);
                    break;
                case AstKind.DynamicBlock:
                    {
                        int block = (node as DynamicBlockNode).block;
                        EmitDynamicBlockNode(block);
                        break;
                    }
                case AstKind.ThisPointer:
                    EmitThisPointerNode();
                    break;
                case AstKind.Dynamic:
                    EmitDynamicNode();
                    break;
                case AstKind.GroupExpression:
                    EmitGroupExpressionNode(node);
                    break;
            }
            return t;
        }

        private void EmitOpCode(OpCode opCode)
        {
            ilGen.Emit(opCode);
            writer.WriteLine(opCode);
        }

        private void EmitOpCode(OpCode opCode, Type t)
        {
            ilGen.Emit(opCode, t);
            writer.WriteLine($"{opCode} {t}");
        }

        private void EmitOpCode(OpCode opCode, int index)
        {
            ilGen.Emit(opCode, index);
            writer.WriteLine($"{opCode} {index}");
        }

        private void EmitOpCode(OpCode opCode, string str)
        {
            ilGen.Emit(opCode, str);
            writer.WriteLine($"{opCode} {str}");
        }

        private void EmitOpCode(OpCode opCode, MethodInfo mInfo)
        {
            ilGen.Emit(opCode, mInfo);
            writer.WriteLine($"{opCode} {mInfo}");
        }

        private void EmitOpCode(OpCode opCode, double val)
        {
            ilGen.Emit(opCode, val);
            writer.WriteLine($"{opCode} {val}");
        }

        private void EmitGroupExpressionNode(AssociativeNode node)
        {
            throw new NotImplementedException();
        }

        private void EmitDynamicNode()
        {
            throw new NotImplementedException();
        }

        private void EmitThisPointerNode()
        {
            throw new NotImplementedException();
        }

        private void EmitDynamicBlockNode(int block)
        {
            throw new NotImplementedException();
        }

        private void EmitImportNode(AssociativeNode node)
        {
            throw new NotImplementedException();
        }

        private void EmitBinaryExpressionNode(AssociativeNode node)
        {
            var bNode = node as BinaryExpressionNode;
            if (bNode == null) throw new ArgumentException("AST node must be a Binary Expression");

            if (bNode.Optr == ProtoCore.DSASM.Operator.assign)
            {
                //var ti = new TypeInference();
                //var t = ti.DfsTraverse(bNode.RightNode);

                var t = DfsTraverse(bNode.RightNode);
                var lNode = bNode.LeftNode as IdentifierNode;
                if(lNode == null)
                {
                    throw new Exception("Left node is expected to be an identifier.");
                }
                if(variables.ContainsKey(lNode.Value))
                {
                    // variable being assigned already exists in dictionary.
                    throw new Exception("Variable redefinition is not allowed.");
                }
                variables.Add(lNode.Value, new Tuple<int, Type>(++localVarIndex, t));
                ilGen.DeclareLocal(t);
                writer.WriteLine($"{nameof(ilGen.DeclareLocal)} {t}");

                EmitOpCode(OpCodes.Stloc, localVarIndex);
                // Add variable to output dictionary: output.Add("varName", variable);
                EmitOpCode(OpCodes.Ldarg_1);
                EmitOpCode(OpCodes.Ldstr, lNode.Value);
                // if t is a single value, wrap it in an array of the single value.
                if (!typeof(IEnumerable).IsAssignableFrom(t))
                {
                    EmitOpCode(OpCodes.Ldc_I4_1);
                    EmitOpCode(OpCodes.Newarr, t);
                    EmitOpCode(OpCodes.Dup);
                    EmitOpCode(OpCodes.Ldc_I4_0);
                    EmitOpCode(OpCodes.Ldloc, localVarIndex);

                    if (t == typeof(int))
                        EmitOpCode(OpCodes.Stelem_I4);
                    else if (t == typeof(long))
                        EmitOpCode(OpCodes.Stelem_I8);
                    else if (t == typeof(double))
                        EmitOpCode(OpCodes.Stelem_R8);
                    else if (t == typeof(bool))
                        EmitOpCode(OpCodes.Stelem_I1);
                    else if (t == typeof(char))
                        EmitOpCode(OpCodes.Stelem_I2);
                    else
                        EmitOpCode(OpCodes.Stelem_Ref);
                }
                else
                {
                    EmitOpCode(OpCodes.Ldloc, localVarIndex);
                }
                var mInfo = typeof(IDictionary<string, IList>).GetMethod(nameof(IDictionary<string, IList>.Add));
                EmitOpCode(OpCodes.Callvirt, mInfo);
            }
            else if(bNode.Optr == ProtoCore.DSASM.Operator.add)
            {
                DfsTraverse(bNode.LeftNode);
                DfsTraverse(bNode.RightNode);

                EmitOpCode(OpCodes.Add);
            }
            // TODO: add Emit calls for other binary operators

        }

        private void EmitUnaryExpressionNode(AssociativeNode node)
        {
            throw new NotImplementedException();
        }

        private void EmitInlineConditionalNode(AssociativeNode node)
        {
            throw new NotImplementedException();
        }

        private Type EmitIdentifierListNode(AssociativeNode node)
        {
            var iln = node as IdentifierListNode;
            if(iln == null) throw new ArgumentException("AST node must be an Identifier List.");

            var ident = iln.LeftNode as IdentifierNode;
            if (ident == null) throw new ArgumentException("Left node of IdentifierListNode is expected to be an identifier.");

            className = ident.Value;
            // Emit className for input to call to ReplicationLogic
            EmitOpCode(OpCodes.Ldstr, className);
            return DfsTraverse(iln.RightNode);
        }

        private void EmitExprListNode(AssociativeNode node)
        {
            throw new NotImplementedException();
        }

        private Type EmitFunctionCallNode(AssociativeNode node)
        {
            var fcn = node as FunctionCallNode;
            if (fcn == null) throw new ArgumentException("AST node must be a Function Call Node.");

            methodName = fcn.Function.Name;
            // Emit methodName for input to call to ReplicationLogic
            EmitOpCode(OpCodes.Ldstr, methodName);

            // Emit args for input to call to ReplicationLogic

            var args = fcn.FormalArguments;
            var numArgs = args.Count;
            EmitOpCode(OpCodes.Ldc_I4, numArgs);
            EmitOpCode(OpCodes.Newarr, typeof(object));
            int argCount = -1;
            foreach (var arg in args)
            {
                EmitOpCode(OpCodes.Dup);
                EmitOpCode(OpCodes.Ldc_I4, ++argCount);
                var t = DfsTraverse(arg);
                if (t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(bool) || t == typeof(char))
                {
                    EmitOpCode(OpCodes.Box, t);
                }
                EmitOpCode(OpCodes.Stelem_Ref);
            }

            // Emit guides
            EmitOpCode(OpCodes.Ldc_I4, numArgs);
            EmitOpCode(OpCodes.Newarr, typeof(string[]));
            argCount = -1;
            foreach(var arg in args)
            {
                EmitOpCode(OpCodes.Dup);
                EmitOpCode(OpCodes.Ldc_I4, ++argCount);

                var argIdent = arg as IdentifierNode;
                var argGuides = argIdent.ReplicationGuides;
                EmitOpCode(OpCodes.Ldc_I4, argGuides.Count);
                EmitOpCode(OpCodes.Newarr, typeof(string));
                int guideCount = -1;
                foreach(var guide in argGuides)
                {
                    EmitOpCode(OpCodes.Dup);
                    EmitOpCode(OpCodes.Ldc_I4, ++guideCount);

                    EmitOpCode(OpCodes.Ldstr, (guide as ReplicationGuideNode).RepGuide.Name);
                    EmitOpCode(OpCodes.Stelem_Ref);
                }
                EmitOpCode(OpCodes.Stelem_Ref);
            }

            // Emit call to ReplicationLogic
            var mInfo = typeof(Replication).GetMethod(nameof(Replication.ReplicationLogic));
            EmitOpCode(OpCodes.Call, mInfo);

            return typeof(IList);
        }

        private void EmitFunctionDefinitionNode(AssociativeNode node)
        {
            throw new NotImplementedException();
        }

        private void EmitConstructorDefinitionNode(AssociativeNode node)
        {
            throw new NotImplementedException();
        }

        private void EmitClassDeclNode(AssociativeNode node)
        {
            throw new NotImplementedException();
        }

        private void EmitLanguageBlockNode(AssociativeNode node)
        {
            throw new NotImplementedException();
        }

        private void EmitRangeExprNode(AssociativeNode node)
        {
            throw new NotImplementedException();
        }

        private Type EmitNullNode(AssociativeNode node)
        {
            if(node is NullNode nullNode)
            {
                EmitOpCode(OpCodes.Ldnull);
                return null;
            }
            throw new ArgumentException("null node is expected.");
        }

        private void EmitDefaultArgNode()
        {
            throw new NotImplementedException();
        }

        private Type EmitStringNode(AssociativeNode node)
        {
            if (node is StringNode strNode)
            {
                EmitOpCode(OpCodes.Ldstr, strNode.Value);
                return typeof(string);
            }
            throw new ArgumentException("string node is expected.");
        }

        private Type EmitCharNode(AssociativeNode node)
        {
            if(node is CharNode charNode)
            {
                EmitOpCode(OpCodes.Ldc_I4_S, charNode.Value[0]);
                return typeof(char);
            }
            throw new ArgumentException("char node is expected.");
        }

        private Type EmitBooleanNode(AssociativeNode node)
        {
            if (node is BooleanNode boolNode)
            {
                if (boolNode.Value)
                    EmitOpCode(OpCodes.Ldc_I4_1);
                else
                    EmitOpCode(OpCodes.Ldc_I4_0);
                return typeof(bool);
            }
            throw new ArgumentException("bool node is expected.");
        }

        private Type EmitDoubleNode(AssociativeNode node)
        {
            if(node is DoubleNode dblNode)
            {
                EmitOpCode(OpCodes.Ldc_R8, dblNode.Value);
                return typeof(double);
            }
            throw new ArgumentException("double node is expected.");
        }

        private Type EmitIntNode(AssociativeNode node)
        {
            if(node is IntNode intNode)
            {
                EmitOpCode(OpCodes.Ldc_I8, intNode.Value);
                return typeof(long);
            }
            throw new ArgumentException("Int node is expected.");
        }

        private Type EmitIdentifierNode(AssociativeNode node)
        {
            // only handle identifiers on rhs of assignment expression for now.
            if(node is IdentifierNode idNode)
            {
                // local variables on rhs of expression should have already been defined.
                Tuple<int, Type> tup;
                int index = -1;
                if(!variables.TryGetValue(idNode.Value, out tup))
                {
                    throw new Exception("Variable is undefined!");
                }
                EmitOpCode(OpCodes.Ldloc, tup.Item1);
                return tup.Item2;
            }
            throw new ArgumentException("Identifier node expected.");
        }

    }

    //public class TypeInference
    //{

    //    public void Emit(List<AssociativeNode> astList)
    //    {
    //        foreach (var ast in astList)
    //        {
    //            DfsTraverse(ast);
    //        }
    //    }

    //    public Type DfsTraverse(Node n)
    //    {
    //        Type t = null;

    //        if (!(n is AssociativeNode node) || node.skipMe)
    //            return t;

    //        switch (node.Kind)
    //        {
    //            case AstKind.Identifier:
    //            case AstKind.TypedIdentifier:
    //                EmitIdentifierNode(node);
    //                break;
    //            case AstKind.Integer:
    //                EmitIntNode(node);
    //                break;
    //            case AstKind.Double:
    //                EmitDoubleNode(node);
    //                break;
    //            case AstKind.Boolean:
    //                EmitBooleanNode(node);
    //                break;
    //            case AstKind.Char:
    //                EmitCharNode(node);
    //                break;
    //            case AstKind.String:
    //                EmitStringNode(node);
    //                break;
    //            case AstKind.DefaultArgument:
    //                EmitDefaultArgNode();
    //                break;
    //            case AstKind.Null:
    //                EmitNullNode(node);
    //                break;
    //            case AstKind.RangeExpression:
    //                EmitRangeExprNode(node);
    //                break;
    //            case AstKind.LanguageBlock:
    //                EmitLanguageBlockNode(node);
    //                break;
    //            case AstKind.ClassDeclaration:
    //                EmitClassDeclNode(node);
    //                break;
    //            case AstKind.Constructor:
    //                EmitConstructorDefinitionNode(node);
    //                break;
    //            case AstKind.FunctionDefintion:
    //                EmitFunctionDefinitionNode(node);
    //                break;
    //            case AstKind.FunctionCall:
    //                EmitFunctionCallNode(node);
    //                break;
    //            case AstKind.FunctionDotCall:
    //                EmitFunctionCallNode(node);
    //                break;
    //            case AstKind.ExpressionList:
    //                EmitExprListNode(node);
    //                break;
    //            case AstKind.IdentifierList:
    //                EmitIdentifierListNode(node);
    //                break;
    //            case AstKind.InlineConditional:
    //                EmitInlineConditionalNode(node);
    //                break;
    //            case AstKind.UnaryExpression:
    //                EmitUnaryExpressionNode(node);
    //                break;
    //            case AstKind.BinaryExpression:
    //                EmitBinaryExpressionNode(node);
    //                break;
    //            case AstKind.Import:
    //                EmitImportNode(node);
    //                break;
    //            case AstKind.DynamicBlock:
    //                {
    //                    int block = (node as DynamicBlockNode).block;
    //                    EmitDynamicBlockNode(block);
    //                    break;
    //                }
    //            case AstKind.ThisPointer:
    //                EmitThisPointerNode();
    //                break;
    //            case AstKind.Dynamic:
    //                EmitDynamicNode();
    //                break;
    //            case AstKind.GroupExpression:
    //                EmitGroupExpressionNode(node);
    //                break;
    //        }
    //        return t;
    //    }

    //    private void EmitGroupExpressionNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitDynamicNode()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitThisPointerNode()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitDynamicBlockNode(int block)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitImportNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitBinaryExpressionNode(AssociativeNode node)
    //    {
    //        var bNode = node as BinaryExpressionNode;
    //        if (bNode == null) throw new ArgumentException("AST node must be a Binary Expression");

    //        if (bNode.Optr == ProtoCore.DSASM.Operator.assign)
    //        {
    //            // Initialize dynamic method to return result of assignment.

    //            DfsTraverse(bNode.RightNode);
    //            var lNode = bNode.LeftNode as IdentifierNode;
    //            if (lNode == null)
    //            {
    //                throw new Exception("Left node is expected to be an identifier but is not!");
    //            }
                
    //        }
    //        else if (bNode.Optr == ProtoCore.DSASM.Operator.add)
    //        {
    //            DfsTraverse(bNode.LeftNode);
    //            DfsTraverse(bNode.RightNode);

    //        }
    //        // TODO: add Emit calls for other binary operators

    //    }

    //    private void EmitUnaryExpressionNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitInlineConditionalNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitIdentifierListNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitExprListNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitFunctionCallNode(AssociativeNode node)
    //    {
    //        if(node is FunctionCallNode fcn)
    //        {
                
    //        }
    //    }

    //    private void EmitFunctionDefinitionNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitConstructorDefinitionNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitClassDeclNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitLanguageBlockNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void EmitRangeExprNode(AssociativeNode node)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private Type EmitNullNode(AssociativeNode node)
    //    {
    //        return typeof(Nullable);
    //    }

    //    private void EmitDefaultArgNode()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private Type EmitStringNode(AssociativeNode node)
    //    {
    //        return typeof(string);
    //    }

    //    private Type EmitCharNode(AssociativeNode node)
    //    {
    //        return typeof(char);
    //    }

    //    private Type EmitBooleanNode(AssociativeNode node)
    //    {
    //        return typeof(bool);
    //    }

    //    private Type EmitDoubleNode(AssociativeNode node)
    //    {
    //        return typeof(double);
    //    }

    //    private Type EmitIntNode(AssociativeNode node)
    //    {
    //        return typeof(long);
    //    }

    //    private void EmitIdentifierNode(AssociativeNode node)
    //    {
    //        // only handle identifiers on rhs of assignment expression for now.
    //        if (node is IdentifierNode idNode)
    //        {
    //            // local variables on rhs of expression should have already been defined.
                
    //        }
    //    }
    //}
}
