import math
from typing import List

# boxes * math.factorial(uniqueness)
def combi_factor(boxes:int, uniqueness: int):
  # for num in range(1,10):
  results = []
  for num in range(1,uniqueness+1):
    results.append(math.factorial(num))
    # print(math.factorial(num))
  return results

print(combi_factor(50, 10)) # 50 boxes, 2 unique sizes, 100

def combi_range(factorial_list: List[int]):
  init_total = factorial_list[-1] * 50
  # o   = (a+b) / a
  # new = (old + new) / old
  old_totals = []
  old_totals.append(init_total) # [181440000]
  for i,num in enumerate(factorial_list):
    old_total = old_totals[i] - (num*50) # 181440000 - 50 = 18149950 
    old_totals.append(old_total) # [181440000, 181439950]
    print(old_totals[i])
  return

# combi_range(combi_factor(50,10))

def combi_range_origin_plus(factorial_list: List[int], origin: int):
  init_total = factorial_list[-1] * 50 - origin # 98000000
  # o   = (a+b) / a
  # new = (old + new) / old
  old_totals = []
  old_totals.append(init_total) # [181440000]
  for i,num in enumerate(factorial_list):
    old_total = int(old_totals[i] - ((num*50)/2)) # 181440000 - 50 = 97999950 
    old_totals.append(old_total) # [98000000, 97999950]
    print(old_totals[i])
  return

combi_range_origin_plus(combi_factor(50,10), 83439975)

def combi_range_origin_minus(factorial_list: List[int], origin: int):
  init_total = factorial_list[-1] * 50 - origin # 98000000
  # o   = (a+b) / a
  # new = (old + new) / old
  old_totals = []
  old_totals.append(init_total) # [181440000]
  for i,num in enumerate(factorial_list):
    old_total = int(old_totals[i] + ((num*50)/2)) # 181440000 + 50 = 98000050 
    old_totals.append(old_total) # [98000000, 98000050]
    print(old_totals[i])
  return

combi_range_origin_minus(combi_factor(50,10), 83439975)


# complexity pack_efficiency     combination_range      complexity waste: $saving
# (10!*50)      100%          98,000,000 - 98,000,050   ( 1!*50)   waste:    $0
#  (9!*50)      99.8046875%   97,999,950 - 98,000,100   ( 2!*50)   waste:    $6
#  (8!*50)      99.609375%    97,999,800 - 98,000,250   ( 3!*50)   waste:   $12
#  (7!*50)      99.21875%     97,999,200 - 98,000,850   ( 4!*50)   waste:   $23
#  (6!*50)      98.4375%      97,996,200 - 98,003,850   ( 5!*50)   waste:   $47
#  (5!*50)      96.875%       97,978,200 - 98,021,850   ( 6!*50)   waste:   $94
#  (4!*50)      93.75%        97,852,200 - 98,147,850   ( 7!*50)   waste:  $187
#  (3!*50)      87.5%         96,844,200 - 99,155,850   ( 8!*50)   waste:  $375
#  (2!*50)      75%           87,772,200 - 108,227,850  ( 9!*50)   waste:  $750
#  (1!*50)      0-50%                                     (10!*50)   waste: $1500