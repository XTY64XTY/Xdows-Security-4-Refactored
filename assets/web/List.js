function GetListData(a,b) {
const table = document.querySelector(a);

if (table) {
  // 收集所有符合条件的行内容
  const output = Array.from(table.rows)
    .map((row, rowIndex) => {
      const secondCell = row.cells[b];
      // 跳过表头和非内容单元格
      if (!secondCell || secondCell.tagName === 'TH') return null;
      return `${secondCell.textContent.trim()}`;
    })
    .filter(item => item !== null) // 过滤空项
    .join('|||'); // 用 ||| 连接所有行

  // 最终输出结果（如果有内容）
  if (output) {
    return output;
  } else {
  return "Error";
  }
} else {
  return "Error";
}

}