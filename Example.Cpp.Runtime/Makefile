ifndef VIDI_DIR
VIDI_DIR	= ../..
endif

export LD_LIBRARY_PATH+=:$(VIDI_DIR)/bin

TARGET		:= example_cpp_runtime
CXXFLAGS	:= -c -pipe -fPIC -Wall
CXXFLAGS 	+= -I$(VIDI_DIR)/include
LDFLAGS		:= -L$(VIDI_DIR)/bin -lvidi

SOURCES  	:= $(wildcard *.cpp)

OBJECTS		:= $(SOURCES:.cpp=.o)

.PHONY: all
all: $(TARGET)
	
$(TARGET): $(OBJECTS) 
	$(CXX) $(OBJECTS) $(LDFLAGS) -o $@
	
.cpp.o:
	$(CXX) $(CXXFLAGS) $< -o $@

.depends:
	$(CXX) $(CXXFLAGS) -MM $(SOURCES) > $@

.PHONY: clean
clean:
	rm -f $(TARGET) $(OBJECTS)
	rm -f .depends

-include .depends

