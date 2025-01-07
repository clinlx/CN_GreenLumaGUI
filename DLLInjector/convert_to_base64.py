import sys
import os
import base64

def file_to_base64(input_file_path, output_file_path):
    # 读取文件的二进制数据
    with open(input_file_path, 'rb') as file:
        binary_data = file.read()
    
    # 将二进制数据转换为Base64字符串
    base64_data = base64.b64encode(binary_data).decode('utf-8')
    
    # 将Base64字符串写入输出文件
    with open(output_file_path, 'w') as output_file:
        output_file.write(base64_data)

if __name__ == "__main__":
    # 检查命令行参数
    if len(sys.argv) != 2:
        print("Usage: python script.py <input_file_path>")
        sys.exit(1)
    
    input_file_path = sys.argv[1]
    
    # 检查输入文件是否存在
    if not os.path.isfile(input_file_path):
        print(f"Error: The file '{input_file_path}' does not exist.")
        sys.exit(1)
    
    # 生成输出文件路径
    output_file_path = input_file_path + ".b64"
    
    # 调用函数进行转换
    file_to_base64(input_file_path, output_file_path)
    
    print(f"Base64 data has been written to {output_file_path}")