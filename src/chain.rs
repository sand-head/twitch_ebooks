use std::collections::HashMap;

pub struct Chain<'a> {
  nodes: HashMap<(&'a str, &'a str), HashMap<&'a str, usize>>,
}
