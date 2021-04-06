use std::collections::{
  hash_map::Entry::{Occupied, Vacant},
  HashMap,
};

use rand::{thread_rng, Rng};

pub struct MarkovChain {
  transitions: HashMap<Option<String>, HashMap<Option<String>, usize>>,
}
impl Default for MarkovChain {
  fn default() -> Self {
    Self {
      transitions: HashMap::new(),
    }
  }
}

impl MarkovChain {
  /// Adds the given string to the chain.
  pub fn add<S: Into<String>>(&mut self, string: S) {
    let string: String = string.into();
    // split up our incoming string into a vector with `None` prepended & appended
    let strings = {
      let mut strings = vec![None];
      strings.extend(string.split(' ').map(|s| Some(s.to_owned())));
      strings.push(None);
      strings
    };

    // use a window of size 2 to get both "previous" and "next" strings
    for string in strings.windows(2) {
      // get the entry for the "previous" string in the vector
      // if we don't have one yet, we create one and insert it
      let prev_entry = self
        .transitions
        .entry(string[0].to_owned())
        .or_insert_with(|| HashMap::new());

      // add the "next" string to the "previous" entry
      // we use the value to track the number of times
      match prev_entry.entry(string[1].to_owned()) {
        Occupied(mut e) => {
          *e.get_mut() += 1;
        }
        Vacant(e) => {
          e.insert(1);
        }
      };
    }
  }

  /// Gets the next "state" in the chain.
  fn next(&self, previous: &Option<String>) -> Option<String> {
    let sum: usize = self.transitions[&previous].iter().map(|(_, v)| v).sum();
    let mut rng = thread_rng();

    let mut random_value = rng.gen_range(0..sum);
    for (key, &value) in self.transitions[&previous].iter() {
      // check if this random value is less than the current value
      if random_value <= value {
        return key.clone();
      }
      // if not, just subtract the value and continue
      random_value -= value;
    }

    unreachable!("The RNG broke the bounds of the range");
  }

  pub fn generate(&self) -> Option<String> {
    if self.transitions.len() == 0 {
      // whoops! we don't have any transitions, so we can't generate anything
      return None;
    }

    let mut result = Vec::<String>::new();
    let mut previous: Option<String> = None;
    loop {
      // get next string
      previous = self.next(&previous);
      // add next string to result vec, or break if none
      if let Some(next) = &previous {
        result.push(next.clone());
      } else {
        break;
      }
    }

    Some(result.join(" "))
  }
}
